import { useQuery } from '@tanstack/react-query'
import { reasoningApi, type ReasoningTimelineEntryDto } from '@/lib/api/client'

interface Props {
  readonly emailId: string
}

const ENTRY_ICONS: Record<string, string> = {
  AGENT_EXECUTION: '🤖',
  CONFLICT:        '⚡',
  TAXONOMY:        '🏷️',
  REVIEW:          '👤',
  WORKFLOW:        '🔄',
}

const ENTRY_COLORS: Record<string, string> = {
  AGENT_EXECUTION: 'bg-blue-100 text-blue-700 border-blue-200',
  CONFLICT:        'bg-orange-100 text-orange-700 border-orange-200',
  TAXONOMY:        'bg-purple-100 text-purple-700 border-purple-200',
  REVIEW:          'bg-green-100 text-green-700 border-green-200',
  WORKFLOW:        'bg-gray-100 text-gray-700 border-gray-200',
}

function confidenceColor(pct: number): string {
  if (pct >= 85) return 'bg-green-100 text-green-700'
  if (pct >= 70) return 'bg-yellow-100 text-yellow-700'
  return 'bg-red-100 text-red-700'
}

function TimelineEntry({ entry }: Readonly<{ entry: ReasoningTimelineEntryDto }>) {
  const icon   = ENTRY_ICONS[entry.entryType]  ?? '•'
  const colors = ENTRY_COLORS[entry.entryType] ?? 'bg-gray-100 text-gray-700 border-gray-200'
  const rawPct = entry.confidence !== null ? Math.round(entry.confidence * 100) : null
  const pct    = rawPct !== null && Number.isFinite(rawPct) ? rawPct : null

  return (
    <div className="flex gap-3">
      <div className="flex flex-col items-center">
        <div className={`w-8 h-8 rounded-full border flex items-center justify-center text-sm flex-shrink-0 ${colors}`}>
          {icon}
        </div>
        <div className="w-px flex-1 bg-gray-200 mt-1" />
      </div>

      <div className="pb-4 flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div>
            <p className="text-sm font-medium text-gray-800">{entry.title}</p>
            <p className="text-xs text-gray-500">{entry.actor}</p>
          </div>
          <span className="text-xs text-gray-400 whitespace-nowrap flex-shrink-0">
            {new Date(entry.timestamp).toLocaleTimeString()}
          </span>
        </div>

        <p className="text-xs text-gray-600 mt-1">{entry.description}</p>

        <div className="flex items-center gap-2 mt-1.5 flex-wrap">
          {pct !== null && (
            <span className={`text-xs px-1.5 py-0.5 rounded font-mono ${confidenceColor(pct)}`}>
              {pct}%
            </span>
          )}
          {entry.status && (
            <span className="text-xs bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded">
              {entry.status}
            </span>
          )}
        </div>
      </div>
    </div>
  )
}

export function ReasoningTimeline({ emailId }: Props) {
  const { data: timeline, isLoading } = useQuery({
    queryKey: ['workflow-timeline', emailId],
    queryFn: () => reasoningApi.getTimeline(emailId),
    refetchInterval: 5000,
  })

  if (isLoading) {
    return <div className="p-4 text-sm text-gray-500 animate-pulse">Loading timeline…</div>
  }

  if (!timeline?.entries.length) {
    return (
      <div className="p-4 text-sm text-gray-500 text-center">
        No reasoning steps recorded yet.
      </div>
    )
  }

  return (
    <div className="space-y-1">
      <h3 className="text-sm font-semibold text-gray-700 mb-3">Reasoning Timeline</h3>
      <div>
        {timeline.entries.map((entry, i) => (
          <TimelineEntry key={`${entry.timestamp}-${i}`} entry={entry} />
        ))}
      </div>
    </div>
  )
}
