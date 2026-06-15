import { useQuery } from '@tanstack/react-query'
import { reasoningApi, type AgentConflictDto } from '@/lib/api/client'

interface Props {
  readonly emailId: string
}

const CONFLICT_COLORS: Record<string, string> = {
  CATEGORY_MISMATCH: 'bg-orange-50 border-orange-300 text-orange-800',
  LOW_CONFIDENCE:    'bg-yellow-50 border-yellow-300 text-yellow-800',
  MISSING_INFORMATION: 'bg-blue-50 border-blue-300 text-blue-800',
  ROUTING_DISPUTE:   'bg-red-50 border-red-300 text-red-800',
}

function ConfidenceBar({ value, label }: Readonly<{ value: number; label: string }>) {
  const pct = Math.round(value * 100)
  const color = pct >= 85 ? 'bg-green-500' : pct >= 70 ? 'bg-yellow-500' : 'bg-red-500'
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs text-gray-500">{label}</span>
      <div className="flex items-center gap-2">
        <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden">
          <div className={`h-full ${color} rounded-full`} style={{ width: `${pct}%` }} />
        </div>
        <span className="text-xs font-mono font-semibold w-8 text-right">{pct}%</span>
      </div>
    </div>
  )
}

function ConflictCard({ conflict }: Readonly<{ conflict: AgentConflictDto }>) {
  const colors = CONFLICT_COLORS[conflict.conflictType] ?? 'bg-gray-50 border-gray-300 text-gray-800'
  const isResolved = !!conflict.resolvedAt

  return (
    <div className={`border rounded-lg p-4 ${colors} ${isResolved ? 'opacity-60' : ''}`}>
      <div className="flex items-start justify-between gap-2 mb-3">
        <div>
          <span className="text-xs font-semibold uppercase tracking-wide">
            {conflict.conflictType.replace(/_/g, ' ')}
          </span>
          {isResolved && (
            <span className="ml-2 text-xs bg-green-100 text-green-700 px-1.5 py-0.5 rounded">
              Resolved
            </span>
          )}
        </div>
        <span className="text-xs text-gray-500 whitespace-nowrap">
          {new Date(conflict.createdAt).toLocaleTimeString()}
        </span>
      </div>

      <p className="text-sm mb-3">{conflict.description}</p>

      <div className="grid grid-cols-2 gap-3 mb-3">
        <div className="bg-white/60 rounded p-2">
          <p className="text-xs font-medium mb-1">{conflict.sourceAgent}</p>
          {conflict.sourceValue && (
            <p className="text-xs text-gray-600 mb-1">→ {conflict.sourceValue}</p>
          )}
          <ConfidenceBar value={conflict.sourceConfidence} label="Confidence" />
        </div>
        <div className="bg-white/60 rounded p-2">
          <p className="text-xs font-medium mb-1">{conflict.targetAgent}</p>
          {conflict.targetValue && (
            <p className="text-xs text-gray-600 mb-1">→ {conflict.targetValue}</p>
          )}
          <ConfidenceBar value={conflict.targetConfidence} label="Confidence" />
        </div>
      </div>

      {conflict.resolution && (
        <div className="text-xs bg-white/60 rounded p-2">
          <span className="font-medium">Resolution: </span>{conflict.resolution}
        </div>
      )}
    </div>
  )
}

export function AgentCollaborationView({ emailId }: Props) {
  const { data: conflicts, isLoading } = useQuery({
    queryKey: ['workflow-conflicts', emailId],
    queryFn: () => reasoningApi.getConflicts(emailId),
  })

  if (isLoading) {
    return (
      <div className="p-4 text-sm text-gray-500 animate-pulse">Loading conflicts…</div>
    )
  }

  if (!conflicts?.length) {
    return (
      <div className="p-4 text-sm text-gray-500 text-center">
        No agent conflicts detected for this workflow.
      </div>
    )
  }

  const active   = conflicts.filter(c => !c.resolvedAt)
  const resolved = conflicts.filter(c => c.resolvedAt)

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-gray-700">Agent Conflicts</h3>
        <div className="flex gap-2 text-xs">
          {active.length > 0 && (
            <span className="bg-red-100 text-red-700 px-2 py-0.5 rounded-full font-medium">
              {active.length} active
            </span>
          )}
          {resolved.length > 0 && (
            <span className="bg-green-100 text-green-700 px-2 py-0.5 rounded-full font-medium">
              {resolved.length} resolved
            </span>
          )}
        </div>
      </div>

      {active.map(c => <ConflictCard key={c.id} conflict={c} />)}
      {resolved.map(c => <ConflictCard key={c.id} conflict={c} />)}
    </div>
  )
}
