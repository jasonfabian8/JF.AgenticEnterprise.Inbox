import { useEffect, useMemo, useState } from 'react'
import {
  ReactFlow,
  type Node,
  type Edge,
  type NodeProps,
  Handle,
  Position,
  Background,
  BackgroundVariant,
} from '@xyflow/react'
import '@xyflow/react/dist/style.css'
import { useAgentEvents } from '@/lib/signalr/AgentEventContext'
import type { AgentExecutionDto, WorkflowDetail } from '@/lib/api/client'
import { cn } from '@/lib/utils'

// ── Node data type ────────────────────────────────────────────────────────────

interface AgentNodeData extends Record<string, unknown> {
  label: string
  status: 'idle' | 'running' | 'completed' | 'failed'
  sublabel?: string
  confidence?: number
  isStartNode?: boolean
}

// ── Status → style maps ───────────────────────────────────────────────────────

const STATUS_BG: Record<AgentNodeData['status'], string> = {
  completed: 'bg-green-50 border-green-300',
  running:   'bg-blue-50 border-blue-300',
  failed:    'bg-red-50 border-red-300',
  idle:      'bg-white border-gray-200',
}

const STATUS_DOT: Record<AgentNodeData['status'], string> = {
  completed: 'bg-green-500',
  running:   'bg-blue-500 animate-pulse',
  failed:    'bg-red-500',
  idle:      'bg-gray-300',
}

const BAR_COLOR = (pct: number) => {
  if (pct >= 80) return 'bg-green-500'
  if (pct >= 50) return 'bg-amber-400'
  return 'bg-red-400'
}

// ── Custom node renderer ──────────────────────────────────────────────────────

function AgentNode({ data }: Readonly<NodeProps<Node<AgentNodeData>>>) {
  const bgClass = data.isStartNode ? 'bg-blue-50 border-blue-200' : STATUS_BG[data.status]
  const dotClass = STATUS_DOT[data.status]
  const pct = data.confidence != null ? Math.round(Number(data.confidence) * 100) : null

  return (
    <div className={cn('rounded-lg border px-3 py-2.5 shadow-sm w-48', bgClass)}>
      <Handle type="target" position={Position.Top} className="!opacity-0 !w-0.5 !h-0.5" />

      <div className="flex items-center gap-2">
        {data.isStartNode ? null : (
          <span className={cn('h-2 w-2 rounded-full flex-shrink-0', dotClass)} />
        )}
        <span className="text-sm font-semibold text-gray-800 leading-tight truncate">
          {String(data.label)}
        </span>
      </div>

      {data.sublabel ? (
        <p className="text-xs text-gray-500 mt-0.5 truncate pl-4">{String(data.sublabel)}</p>
      ) : null}

      {pct != null && data.status === 'completed' ? (
        <div className="mt-1.5 pl-4">
          <div className="h-1 w-full rounded-full bg-gray-200">
            <div
              className={cn('h-1 rounded-full', BAR_COLOR(pct))}
              style={{ width: `${pct}%` }}
            />
          </div>
          <p className="text-xs text-gray-400 mt-0.5">{pct}% conf</p>
        </div>
      ) : null}

      <Handle type="source" position={Position.Bottom} className="!opacity-0 !w-0.5 !h-0.5" />
    </div>
  )
}

const nodeTypes = { agentNode: AgentNode }

// ── Helpers ───────────────────────────────────────────────────────────────────

type NodeStatus = AgentNodeData['status']

const TERMINAL_STATUSES = new Set(['FAILED', 'COMPLETED_AUTO', 'COMPLETED_HUMAN'])

function resolveStatus(
  executions: AgentExecutionDto[],
  agentType: string,
  liveRunning: Set<string>,
): NodeStatus {
  if (liveRunning.has(agentType)) return 'running'
  const e = executions.find(x => x.agentType === agentType)
  if (!e) return 'idle'
  if (e.status === 'COMPLETED') return 'completed'
  if (e.status === 'FAILED') return 'failed'
  if (e.status === 'RUNNING') return 'running'
  return 'idle'
}

const NEXT_AGENT_LABELS: Record<string, string> = {
  InvoiceAgent:  'Invoice Agent',
  ContractAgent: 'Contract Agent',
  HumanReview:   'Human Review',
  Complete:      'Complete',
}

function labelForNextAgent(nextAgent: string | undefined): string | null {
  if (!nextAgent) return null
  return NEXT_AGENT_LABELS[nextAgent] ?? nextAgent
}

function categoryFromExecution(executions: AgentExecutionDto[]): string | null {
  const e = executions.find(x => x.agentType === 'ClassificationAgent')
  if (!e?.outputPayloadJson) return null
  try {
    return (JSON.parse(e.outputPayloadJson) as { category?: string }).category ?? null
  } catch {
    return null
  }
}

function classificationSublabel(
  category: string | null,
  status: NodeStatus,
): string | undefined {
  if (category) return category
  if (status === 'running') return 'Analyzing…'
  return undefined
}

function orchestratorSublabel(
  specLabel: string | null,
  status: NodeStatus,
): string | undefined {
  if (specLabel) return `→ ${specLabel}`
  if (status === 'running') return 'Routing…'
  return 'Awaiting'
}

function specializedAgentType(nextAgent: string | undefined): string | null {
  if (nextAgent === 'InvoiceAgent') return 'InvoiceAgent'
  if (nextAgent === 'ContractAgent') return 'ContractAgent'
  return null
}

// ── Graph builder ─────────────────────────────────────────────────────────────

function buildGraph(
  workflow: WorkflowDetail | undefined,
  emailSubject: string,
  liveRunning: Set<string>,
): { nodes: Node[]; edges: Edge[] } {
  const executions = workflow?.agentExecutions ?? []
  const decision = workflow?.orchestrationDecision
  const truncate = (s: string) => (s.length > 30 ? `${s.slice(0, 30)}…` : s)

  const classStatus = resolveStatus(executions, 'ClassificationAgent', liveRunning)
  const orchStatus  = resolveStatus(executions, 'OrchestratorAgent', liveRunning)
  const category    = categoryFromExecution(executions)
  const classExec   = executions.find(e => e.agentType === 'ClassificationAgent')

  const nextAgent   = decision?.nextAgent
  const specLabel   = labelForNextAgent(nextAgent)
  const specType    = specializedAgentType(nextAgent)
  const specStatus: NodeStatus = specType ? resolveStatus(executions, specType, liveRunning) : 'idle'
  const specExec    = specType ? executions.find(e => e.agentType === specType) : null

  const nodes: Node[] = [
    {
      id: 'email',
      position: { x: 110, y: 0 },
      type: 'agentNode',
      data: {
        label: 'Email',
        sublabel: truncate(emailSubject || '(no subject)'),
        status: 'completed',
        isStartNode: true,
      } satisfies AgentNodeData,
    },
    {
      id: 'classification',
      position: { x: 110, y: 130 },
      type: 'agentNode',
      data: {
        label: 'Classification Agent',
        sublabel: classificationSublabel(category, classStatus),
        status: classStatus,
        confidence: classExec?.confidenceScore ?? undefined,
      } satisfies AgentNodeData,
    },
    {
      id: 'orchestrator',
      position: { x: 110, y: 260 },
      type: 'agentNode',
      data: {
        label: 'Orchestrator Agent',
        sublabel: orchestratorSublabel(specLabel, orchStatus),
        status: orchStatus,
      } satisfies AgentNodeData,
    },
  ]

  const edges: Edge[] = [
    {
      id: 'e1',
      source: 'email',
      target: 'classification',
      type: 'smoothstep',
      animated: classStatus === 'running',
      style: { stroke: '#d1d5db', strokeWidth: 1.5 },
    },
    {
      id: 'e2',
      source: 'classification',
      target: 'orchestrator',
      type: 'smoothstep',
      animated: orchStatus === 'running',
      style: { stroke: '#d1d5db', strokeWidth: 1.5 },
    },
  ]

  if (specLabel && orchStatus !== 'idle') {
    nodes.push({
      id: 'specialized',
      position: { x: 110, y: 390 },
      type: 'agentNode',
      data: {
        label: specLabel,
        sublabel: specStatus === 'running' ? 'Processing…' : undefined,
        status: specStatus,
        confidence: specExec?.confidenceScore ?? undefined,
      } satisfies AgentNodeData,
    })
    edges.push({
      id: 'e3',
      source: 'orchestrator',
      target: 'specialized',
      type: 'smoothstep',
      animated: specStatus === 'running',
      style: { stroke: '#d1d5db', strokeWidth: 1.5 },
    })
  }

  return { nodes, edges }
}

// ── Component ─────────────────────────────────────────────────────────────────

interface Props {
  workflow: WorkflowDetail | undefined
  emailSubject: string
}

export function WorkflowGraph({ workflow, emailSubject }: Readonly<Props>) {
  const { onAgentStarted } = useAgentEvents()
  const [liveRunning, setLiveRunning] = useState<Set<string>>(new Set())

  // Track agents that started but haven't completed yet
  useEffect(() => {
    if (!workflow?.workflowId) return
    const wfId = workflow.workflowId
    return onAgentStarted(p => {
      if (p.workflowId !== wfId) return
      setLiveRunning(prev => new Set([...prev, p.agent]))
    })
  }, [workflow?.workflowId, onAgentStarted])

  // Clear all live state when the workflow reaches a terminal status
  useEffect(() => {
    if (workflow?.status && TERMINAL_STATUSES.has(workflow.status)) {
      setLiveRunning(new Set())
    }
  }, [workflow?.status])

  // Clear individual agents once their execution is persisted as done
  useEffect(() => {
    const done = new Set(
      (workflow?.agentExecutions ?? [])
        .filter(e => e.status === 'COMPLETED' || e.status === 'FAILED')
        .map(e => e.agentType),
    )
    if (done.size === 0) return
    setLiveRunning(prev => {
      const next = new Set(prev)
      done.forEach(a => next.delete(a))
      return next.size === prev.size ? prev : next
    })
  }, [workflow?.agentExecutions])

  const { nodes, edges } = useMemo(
    () => buildGraph(workflow, emailSubject, liveRunning),
    [workflow, emailSubject, liveRunning],
  )

  const hasSpecialized = nodes.length > 3
  const containerHeight = hasSpecialized ? 480 : 350

  return (
    <div
      className="w-full rounded-lg overflow-hidden border border-gray-100 bg-slate-50"
      style={{ height: containerHeight }}
    >
      <ReactFlow
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        fitView
        fitViewOptions={{ padding: 0.25, maxZoom: 1.1 }}
        nodesDraggable={false}
        nodesConnectable={false}
        elementsSelectable={false}
        panOnDrag={false}
        zoomOnScroll={false}
        zoomOnPinch={false}
        preventScrolling={false}
        proOptions={{ hideAttribution: true }}
      >
        <Background variant={BackgroundVariant.Dots} gap={18} color="#e2e8f0" />
      </ReactFlow>
    </div>
  )
}
