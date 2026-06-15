import { cn } from '@/lib/utils'

interface WorkflowNode {
  label: string
  sublabel?: string
  type: 'email' | 'agent' | 'result'
  status?: 'pending' | 'running' | 'completed' | 'failed'
}

const TYPE_STYLES: Record<WorkflowNode['type'], string> = {
  email:  'bg-blue-50 border-blue-200 text-blue-800',
  agent:  'bg-white border-gray-200 text-gray-800',
  result: 'bg-green-50 border-green-200 text-green-800',
}

const STATUS_RING: Record<string, string> = {
  running:   'ring-2 ring-blue-400 ring-offset-1',
  completed: 'ring-2 ring-green-400 ring-offset-1',
  failed:    'ring-2 ring-red-400 ring-offset-1',
}

const STATUS_DOT: Record<string, string> = {
  pending:   'bg-gray-300',
  running:   'bg-blue-500 animate-pulse',
  completed: 'bg-green-500',
  failed:    'bg-red-500',
}

function Node({ node }: { node: WorkflowNode }) {
  const ringClass  = node.status ? (STATUS_RING[node.status] ?? '') : ''
  const typeClass  = TYPE_STYLES[node.type]
  const dotClass   = node.status ? (STATUS_DOT[node.status] ?? '') : null

  return (
    <div
      className={cn(
        'relative flex flex-col items-center rounded-lg border px-4 py-2.5 text-center shadow-sm',
        typeClass,
        ringClass,
      )}
      style={{ minWidth: '160px' }}
    >
      {dotClass && (
        <span
          className={cn(
            'absolute -top-1.5 -right-1.5 h-3 w-3 rounded-full border-2 border-white',
            dotClass,
          )}
        />
      )}
      <p className="text-sm font-semibold leading-tight">{node.label}</p>
      {node.sublabel && (
        <p className="mt-0.5 text-xs opacity-70">{node.sublabel}</p>
      )}
    </div>
  )
}

function Arrow() {
  return (
    <div className="flex flex-col items-center py-0.5">
      <div className="h-5 w-px bg-gray-300" />
      <svg width="10" height="6" viewBox="0 0 10 6" className="text-gray-300 fill-current">
        <path d="M5 6L0 0h10L5 6z" />
      </svg>
    </div>
  )
}

// ── Public component ──────────────────────────────────────────────────────────

interface Props {
  emailSubject: string
  agentStatus?: 'pending' | 'running' | 'completed' | 'failed'
  classificationCategory?: string | null
  classificationConfidence?: number | null
}

export function WorkflowVisualization({
  emailSubject,
  agentStatus = 'pending',
  classificationCategory,
  classificationConfidence,
}: Props) {
  const hasResult = agentStatus === 'completed' && classificationCategory

  const confidenceSuffix =
    classificationConfidence != null
      ? ` · ${Math.round(classificationConfidence * 100)}%`
      : ''

  return (
    <div className="flex flex-col items-center gap-0 py-2">
      <Node
        node={{
          label: 'Email',
          sublabel: emailSubject.length > 40 ? emailSubject.slice(0, 40) + '…' : emailSubject,
          type: 'email',
        }}
      />
      <Arrow />
      <Node
        node={{
          label: 'Classification Agent',
          sublabel: agentStatus === 'running' ? 'Analyzing…' : agentStatus,
          type: 'agent',
          status: agentStatus,
        }}
      />
      {hasResult && (
        <>
          <Arrow />
          <Node
            node={{
              label: classificationCategory!,
              sublabel: `Classified${confidenceSuffix}`,
              type: 'result',
              status: 'completed',
            }}
          />
        </>
      )}
      {agentStatus === 'failed' && (
        <>
          <Arrow />
          <Node
            node={{
              label: 'Classification Failed',
              sublabel: 'See error details below',
              type: 'result',
              status: 'failed',
            }}
          />
        </>
      )}
    </div>
  )
}
