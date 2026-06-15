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
})

// ── Provider ──────────────────────────────────────────────────────────────────

export function AgentEventProvider({ children }: { children: ReactNode }) {
  const [isConnected, setIsConnected] = useState(false)
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/inbox')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.onreconnecting(() => setIsConnected(false))
    connection.onreconnected(() => setIsConnected(true))
    connection.onclose(() => setIsConnected(false))

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch(err => console.error('[SignalR] Connection failed:', err))

    connectionRef.current = connection

    return () => {
      connection.stop()
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
    }),
    [isConnected, joinWorkflow, leaveWorkflow, onAgentStarted, onAgentCompleted, onAgentFailed, onWorkflowUpdated, onWorkflowCompleted],
  )

  return (
    <AgentEventContext.Provider value={value}>
      {children}
    </AgentEventContext.Provider>
  )
}

export const useAgentEvents = () => useContext(AgentEventContext)
