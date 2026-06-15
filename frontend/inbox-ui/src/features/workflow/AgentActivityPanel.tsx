import { useEffect, useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { workflowApi, type AgentExecutionDto } from '@/lib/api/client'
import { useAgentEvents, type AgentCompletedPayload, type AgentFailedPayload, type AgentStartedPayload } from '@/lib/signalr/AgentEventContext'
import { cn } from '@/lib/utils'

// ── Local live-event state ────────────────────────────────────────────────────

interface LiveEvent {
  agent: string
  status: 'running' | 'completed' | 'failed'
  category?: string
  confidence?: number
  reasoning?: string
  error?: string
}

// ── Status styles ─────────────────────────────────────────────────────────────

const STATUS_DOT: Record<string, string> = {
  PENDING:   'bg-gray-300',
  RUNNING:   'bg-blue-500 animate-pulse',
  COMPLETED: 'bg-green-500',
  FAILED:    'bg-red-500',
}

const STATUS_LABEL: Record<string, string> = {
  PENDING:   'Pending',
  RUNNING:   'Running…',
  COMPLETED: 'Completed',
  FAILED:    'Failed',
}

function fmtMs(ms: number) {
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(2)} s`
}

function ConfidenceBar({ value }: { value: number }) {
  const pct = Math.round(value * 100)
  const color = pct >= 80 ? 'bg-green-500' : pct >= 50 ? 'bg-amber-400' : 'bg-red-400'
  return (
    <div className="mt-2">
      <div className="flex justify-between text-xs text-gray-400 mb-1">
        <span>Confidence</span>
        <span className="font-medium text-gray-700">{pct}%</span>
      </div>
      <div className="h-1.5 w-full rounded-full bg-gray-100">
        <div className={cn('h-1.5 rounded-full transition-all duration-500', color)} style={{ width: `${pct}%` }} />
      </div>
    </div>
  )
}

// ── Execution card ────────────────────────────────────────────────────────────

function ExecutionCard({
  execution,
  live,
}: {
  execution?: AgentExecutionDto
  live?: LiveEvent
}) {
  const status  = live?.status?.toUpperCase() ?? execution?.status ?? 'PENDING'
  const agent   = live?.agent ?? execution?.agentType ?? 'Unknown Agent'
  const label   = agent.replace('Agent', ' Agent').trim()
  const dotClass = STATUS_DOT[status] ?? 'bg-gray-300'

  const confidence = live?.confidence ?? (execution?.confidenceScore ?? null)
  const reasoning  = live?.reasoning  ?? execution?.reasoningText ?? null
  const error      = live?.error      ?? execution?.errorMessage  ?? null
  const durationMs = execution?.durationMs ?? 0

  let categoryLine: string | null = null
  if (live?.category) {
    categoryLine = live.category
  } else if (execution?.outputPayloadJson) {
    try {
      const parsed = JSON.parse(execution.outputPayloadJson) as { category?: string }
      categoryLine = parsed.category ?? null
    } catch {
      categoryLine = null
    }
  }

  return (
    <div className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
      {/* Header row */}
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 min-w-0">
          <span className={cn('h-2.5 w-2.5 shrink-0 rounded-full', dotClass)} />
          <span className="text-sm font-semibold text-gray-800 truncate">{label}</span>
        </div>
        <div className="flex items-center gap-2 shrink-0 text-xs text-gray-400">
          <span>{STATUS_LABEL[status] ?? status}</span>
          {durationMs > 0 && <span>· {fmtMs(durationMs)}</span>}
        </div>
      </div>

      {/* Category result */}
      {categoryLine && (
        <p className="mt-2 text-sm font-medium text-blue-600">
          {categoryLine}
        </p>
      )}

      {/* Confidence bar */}
      {confidence != null && confidence > 0 && (
        <ConfidenceBar value={confidence} />
      )}

      {/* Reasoning */}
      {reasoning && (
        <p className="mt-2 text-xs leading-relaxed text-gray-500">{reasoning}</p>
      )}

      {/* Error */}
      {error && (
        <p className="mt-2 text-xs text-red-500 break-words">{error}</p>
      )}
    </div>
  )
}

// ── Main panel ────────────────────────────────────────────────────────────────

interface Props {
  workflowId: string
  emailId: string
}

export function AgentActivityPanel({ workflowId, emailId }: Props) {
  const queryClient = useQueryClient()
  const { joinWorkflow, leaveWorkflow, onAgentStarted, onAgentCompleted, onAgentFailed } =
    useAgentEvents()

  const [liveEvents, setLiveEvents] = useState<Map<string, LiveEvent>>(new Map())
  const joined = useRef(false)

  // Poll persisted executions (refreshed when a live event arrives)
  const { data } = useQuery({
    queryKey: ['workflow-executions', workflowId],
    queryFn:  () => workflowApi.getExecutions(workflowId),
    retry: false,
    refetchInterval: liveEvents.size > 0 ? 3000 : false,
  })

  // Join SignalR workflow group
  useEffect(() => {
    if (joined.current) return
    joined.current = true
    joinWorkflow(workflowId)
    return () => { leaveWorkflow(workflowId) }
  }, [workflowId, joinWorkflow, leaveWorkflow])

  // Subscribe to live events
  useEffect(() => {
    const unsub1 = onAgentStarted((p: AgentStartedPayload) => {
      if (p.workflowId !== workflowId) return
      setLiveEvents(prev => new Map(prev).set(p.agent, { agent: p.agent, status: 'running' }))
    })

    const unsub2 = onAgentCompleted((p: AgentCompletedPayload) => {
      if (p.workflowId !== workflowId) return
      setLiveEvents(prev =>
        new Map(prev).set(p.agent, {
          agent:      p.agent,
          status:     'completed',
          category:   p.category,
          confidence: p.confidence,
          reasoning:  p.reasoning,
        }),
      )
      // Invalidate all queries that display this email's data
      void queryClient.invalidateQueries({ queryKey: ['email', emailId] })
      void queryClient.invalidateQueries({ queryKey: ['workflow', emailId] })
      void queryClient.invalidateQueries({ queryKey: ['workflow-executions', workflowId] })
      void queryClient.invalidateQueries({ queryKey: ['emails'] })
    })

    const unsub3 = onAgentFailed((p: AgentFailedPayload) => {
      if (p.workflowId !== workflowId) return
      setLiveEvents(prev =>
        new Map(prev).set(p.agent, { agent: p.agent, status: 'failed', error: p.error }),
      )
      void queryClient.invalidateQueries({ queryKey: ['workflow-executions', workflowId] })
    })

    return () => { unsub1(); unsub2(); unsub3() }
  }, [workflowId, emailId, onAgentStarted, onAgentCompleted, onAgentFailed, queryClient])

  // Merge live events + persisted executions for display
  const persistedByAgent = new Map(
    (data?.executions ?? []).map(e => [e.agentType, e]),
  )

  // All agents to show: union of live + persisted
  const agentKeys = new Set([
    ...liveEvents.keys(),
    ...persistedByAgent.keys(),
  ])

  if (agentKeys.size === 0) {
    return (
      <p className="text-xs italic text-gray-400 py-2">
        Waiting for agents to start…
      </p>
    )
  }

  return (
    <div className="space-y-3">
      {[...agentKeys].map(agent => (
        <ExecutionCard
          key={agent}
          execution={persistedByAgent.get(agent)}
          live={liveEvents.get(agent)}
        />
      ))}
    </div>
  )
}
