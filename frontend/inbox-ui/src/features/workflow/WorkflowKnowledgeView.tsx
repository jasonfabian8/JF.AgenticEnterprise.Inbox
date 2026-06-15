import { useEffect, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { reasoningApi, type WorkflowKnowledgeDto } from '@/lib/api/client'

interface Props {
  readonly emailId: string
}

type Phase = {
  key: string
  label: string
  category: string | null
  confidence: number | null
  reasoning: string | null
  actor?: string | null
  at?: string | null
}

function PhaseCard({
  phase,
  isCurrent,
  isActive,
}: Readonly<{ phase: Phase; isCurrent: boolean; isActive: boolean }>) {
  const pct = phase.confidence === null ? null : Math.round(phase.confidence * 100)
  let barColor = 'bg-gray-300'
  if (pct !== null) {
    if (pct >= 85) barColor = 'bg-green-500'
    else if (pct >= 70) barColor = 'bg-yellow-500'
    else barColor = 'bg-red-500'
  }
  const barRef = useRef<HTMLDivElement>(null)
  useEffect(() => { if (barRef.current && pct !== null) barRef.current.style.width = `${pct}%` }, [pct])

  return (
    <div
      className={`
        border rounded-lg p-3 transition-all
        ${isActive ? 'border-blue-400 bg-blue-50' : 'border-gray-200 bg-white'}
        ${isCurrent ? 'ring-2 ring-blue-500' : ''}
        ${phase.category ? '' : 'opacity-40'}
      `}
    >
      <div className="flex items-center justify-between mb-2">
        <span className={`text-xs font-semibold uppercase tracking-wide ${isActive ? 'text-blue-700' : 'text-gray-500'}`}>
          {phase.label}
        </span>
        {isCurrent && (
          <span className="text-xs bg-blue-600 text-white px-1.5 py-0.5 rounded">Current</span>
        )}
      </div>

      {phase.category ? (
        <>
          <p className="text-sm font-medium text-gray-800 mb-2">{phase.category}</p>

          {pct !== null && (
            <div className="mb-2">
              <div className="flex items-center gap-2">
                <div className="flex-1 h-1.5 bg-gray-200 rounded-full overflow-hidden">
                  <div ref={barRef} className={`h-full ${barColor} rounded-full`} />
                </div>
                <span className="text-xs font-mono">{pct}%</span>
              </div>
            </div>
          )}

          {phase.reasoning && (
            <p className="text-xs text-gray-600 line-clamp-2">{phase.reasoning}</p>
          )}

          {phase.actor && (
            <p className="text-xs text-gray-400 mt-1">by {phase.actor}</p>
          )}
        </>
      ) : (
        <p className="text-xs text-gray-400">Not yet reached</p>
      )}
    </div>
  )
}

function KnowledgeTimeline({ knowledge }: Readonly<{ knowledge: WorkflowKnowledgeDto }>) {
  const phases: Phase[] = [
    {
      key: 'initial',
      label: 'Initial (Classification)',
      category: knowledge.initialCategory,
      confidence: knowledge.initialConfidence,
      reasoning: null,
    },
    {
      key: 'refined',
      label: 'Refined (Specialized)',
      category: knowledge.refinedCategory,
      confidence: knowledge.refinedConfidence,
      reasoning: knowledge.refinedReasoning,
    },
    {
      key: 'suggested',
      label: 'Suggested (Taxonomy)',
      category: knowledge.suggestedCategory,
      confidence: knowledge.suggestionConfidence,
      reasoning: knowledge.suggestionReasoning,
    },
    {
      key: 'approved',
      label: 'Approved (Human)',
      category: knowledge.approvedCategory,
      confidence: null,
      reasoning: null,
      actor: knowledge.approvedBy,
      at: knowledge.approvedAt,
    },
  ]

  let currentKey = 'initial'
  if (knowledge.approvedCategory) currentKey = 'approved'
  else if (knowledge.suggestedCategory) currentKey = 'suggested'
  else if (knowledge.refinedCategory) currentKey = 'refined'

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {phases.map(p => (
          <PhaseCard
            key={p.key}
            phase={p}
            isCurrent={p.key === currentKey}
            isActive={!!p.category}
          />
        ))}
      </div>

      <div className="border rounded-lg p-3 bg-gray-50">
        <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-1">
          Current Understanding
        </p>
        <div className="flex items-center gap-2">
          <span className="font-medium text-gray-800">{knowledge.currentCategory}</span>
          <span className="text-xs text-gray-500">
            ({Math.round(knowledge.currentConfidence * 100)}% confidence)
          </span>
        </div>
        {knowledge.currentReasoning && (
          <p className="text-xs text-gray-600 mt-1">{knowledge.currentReasoning}</p>
        )}
      </div>
    </div>
  )
}

export function WorkflowKnowledgeView({ emailId }: Props) {
  const { data: knowledge, isLoading } = useQuery({
    queryKey: ['workflow-knowledge', emailId],
    queryFn: () => reasoningApi.getKnowledge(emailId),
  })

  if (isLoading) {
    return <div className="p-4 text-sm text-gray-500 animate-pulse">Loading knowledge state…</div>
  }

  if (!knowledge) {
    return (
      <div className="p-4 text-sm text-gray-500 text-center">
        No workflow knowledge available yet.
      </div>
    )
  }

  return (
    <div className="space-y-3">
      <h3 className="text-sm font-semibold text-gray-700">Document Understanding Evolution</h3>
      <KnowledgeTimeline knowledge={knowledge} />
    </div>
  )
}
