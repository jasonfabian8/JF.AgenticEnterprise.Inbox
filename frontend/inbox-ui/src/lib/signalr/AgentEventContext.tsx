import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react'
import * as signalR from '@microsoft/signalr'

// ── Event types (mirror the backend payloads) ─────────────────────────────────

export interface AgentStartedPayload {
  workflowId: string
  agent: string
  emailId: string
  timestamp: string
}

export interface AgentCompletedPayload {
  workflowId: string
  agent: string
  emailId: string
  category: string
  confidence: number
  reasoning: string
  timestamp: string
}

export interface AgentFailedPayload {
  workflowId: string
  agent: string
  emailId: string
  error: string
  timestamp: string
}

export interface WorkflowUpdatedPayload {
  workflowId: string
  emailId: string
  status: string
  currentStep: string
  nextAgent: string | null
  timestamp: string
}

export interface WorkflowCompletedPayload {
  workflowId: string
  emailId: string
  finalStatus: string
  classificationCategory: string
  routedToAgent: string
  invoiceAnalysisId: string | null
  contractAnalysisId: string | null
  timestamp: string
}

// ── Sprint 3 event types ──────────────────────────────────────────────────────

export interface ConflictDetectedPayload {
  workflowId: string
  emailId: string
  conflictId: string
  conflictType: string
  sourceAgent: string
  targetAgent: string
  sourceValue: string | null
  targetValue: string | null
  sourceConfidence: number
  targetConfidence: number
  description: string
  timestamp: string
}

export interface TaxonomySuggestedPayload {
  workflowId: string
  emailId: string
  proposalId: string
  suggestedCategory: string
  confidence: number
  reasoning: string
  timestamp: string
}

export interface ReviewRequestedPayload {
  workflowId: string
  emailId: string
  reviewId: string
  reviewType: string
  priority: string
  question: string
  recommendation: string
  timestamp: string
}

export interface ReviewCompletedPayload {
  workflowId: string
  emailId: string
  reviewId: string
  action: string
  reviewerId: string
  overrideCategory: string | null
  timestamp: string
}

// ── Context shape ─────────────────────────────────────────────────────────────

interface AgentEventContextValue {
  isConnected: boolean
  joinWorkflow: (workflowId: string) => Promise<void>
  leaveWorkflow: (workflowId: string) => Promise<void>
  onAgentStarted: (handler: (p: AgentStartedPayload) => void) => () => void
  onAgentCompleted: (handler: (p: AgentCompletedPayload) => void) => () => void
  onAgentFailed: (handler: (p: AgentFailedPayload) => void) => () => void
  onWorkflowUpdated: (handler: (p: WorkflowUpdatedPayload) => void) => () => void
  onWorkflowCompleted: (handler: (p: WorkflowCompletedPayload) => void) => () => void
  // Sprint 3
  onConflictDetected: (handler: (p: ConflictDetectedPayload) => void) => () => void
  onTaxonomySuggested: (handler: (p: TaxonomySuggestedPayload) => void) => () => void
  onReviewRequested: (handler: (p: ReviewRequestedPayload) => void) => () => void
  onReviewCompleted: (handler: (p: ReviewCompletedPayload) => void) => () => void
}

const AgentEventContext = createContext<AgentEventContextValue>({
  isConnected: false,
  joinWorkflow: async () => {},
  leaveWorkflow: async () => {},
  onAgentStarted: () => () => {},
  onAgentCompleted: () => () => {},
  onAgentFailed: () => () => {},
  onWorkflowUpdated: () => () => {},
  onWorkflowCompleted: () => () => {},
  onConflictDetected: () => () => {},
  onTaxonomySuggested: () => () => {},
  onReviewRequested: () => () => {},
  onReviewCompleted: () => () => {},
})

// ── Provider ──────────────────────────────────────────────────────────────────

export function AgentEventProvider({ children }: Readonly<{ children: ReactNode }>) {
  const [isConnected, setIsConnected] = useState(false)
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/inbox')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    let cancelled = false

    connection.onreconnecting(() => { if (!cancelled) setIsConnected(false) })
    connection.onreconnected(() => { if (!cancelled) setIsConnected(true) })
    connection.onclose(() => { if (!cancelled) setIsConnected(false) })

    connectionRef.current = connection

    connection
      .start()
      .then(() => { if (!cancelled) setIsConnected(true) })
      .catch(err => {
        // Suppress errors caused by StrictMode cleanup stopping the connection
        // before negotiation completes — not a real failure in that case.
        if (!cancelled) console.error('[SignalR] Connection failed:', err)
      })

    return () => {
      cancelled = true
      connectionRef.current = null
      connection.stop().catch(() => { /* ignore stop errors on cleanup */ })
    }
  }, [])

  const joinWorkflow = useCallback(async (workflowId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('JoinWorkflow', workflowId)
    }
  }, [])

  const leaveWorkflow = useCallback(async (workflowId: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('LeaveWorkflow', workflowId)
    }
  }, [])

  const onAgentStarted = useCallback(
    (handler: (p: AgentStartedPayload) => void) => {
      connectionRef.current?.on('agent.started', handler)
      return () => connectionRef.current?.off('agent.started', handler)
    },
    [],
  )

  const onAgentCompleted = useCallback(
    (handler: (p: AgentCompletedPayload) => void) => {
      connectionRef.current?.on('agent.completed', handler)
      return () => connectionRef.current?.off('agent.completed', handler)
    },
    [],
  )

  const onAgentFailed = useCallback(
    (handler: (p: AgentFailedPayload) => void) => {
      connectionRef.current?.on('agent.failed', handler)
      return () => connectionRef.current?.off('agent.failed', handler)
    },
    [],
  )

  const onWorkflowUpdated = useCallback(
    (handler: (p: WorkflowUpdatedPayload) => void) => {
      connectionRef.current?.on('workflow.updated', handler)
      return () => connectionRef.current?.off('workflow.updated', handler)
    },
    [],
  )

  const onWorkflowCompleted = useCallback(
    (handler: (p: WorkflowCompletedPayload) => void) => {
      connectionRef.current?.on('workflow.completed', handler)
      return () => connectionRef.current?.off('workflow.completed', handler)
    },
    [],
  )

  const onConflictDetected = useCallback(
    (handler: (p: ConflictDetectedPayload) => void) => {
      connectionRef.current?.on('conflict.detected', handler)
      return () => connectionRef.current?.off('conflict.detected', handler)
    },
    [],
  )

  const onTaxonomySuggested = useCallback(
    (handler: (p: TaxonomySuggestedPayload) => void) => {
      connectionRef.current?.on('taxonomy.suggested', handler)
      return () => connectionRef.current?.off('taxonomy.suggested', handler)
    },
    [],
  )

  const onReviewRequested = useCallback(
    (handler: (p: ReviewRequestedPayload) => void) => {
      connectionRef.current?.on('review.requested', handler)
      return () => connectionRef.current?.off('review.requested', handler)
    },
    [],
  )

  const onReviewCompleted = useCallback(
    (handler: (p: ReviewCompletedPayload) => void) => {
      connectionRef.current?.on('review.completed', handler)
      return () => connectionRef.current?.off('review.completed', handler)
    },
    [],
  )

  const value = useMemo(
    () => ({
      isConnected,
      joinWorkflow,
      leaveWorkflow,
      onAgentStarted,
      onAgentCompleted,
      onAgentFailed,
      onWorkflowUpdated,
      onWorkflowCompleted,
      onConflictDetected,
      onTaxonomySuggested,
      onReviewRequested,
      onReviewCompleted,
    }),
    [
      isConnected, joinWorkflow, leaveWorkflow,
      onAgentStarted, onAgentCompleted, onAgentFailed,
      onWorkflowUpdated, onWorkflowCompleted,
      onConflictDetected, onTaxonomySuggested, onReviewRequested, onReviewCompleted,
    ],
  )

  return (
    <AgentEventContext.Provider value={value}>
      {children}
    </AgentEventContext.Provider>
  )
}

export const useAgentEvents = () => useContext(AgentEventContext)
