import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { reviewApi, type HumanReviewDto, type HumanReviewDecisionRequest } from '@/lib/api/client'

const PRIORITY_ORDER: Record<string, number> = { URGENT: 0, HIGH: 1, MEDIUM: 2, LOW: 3 }
const PRIORITY_COLORS: Record<string, string> = {
  URGENT: 'bg-red-100 text-red-700',
  HIGH:   'bg-orange-100 text-orange-700',
  MEDIUM: 'bg-yellow-100 text-yellow-700',
  LOW:    'bg-gray-100 text-gray-600',
}

function ReviewDecideForm({
  review,
  onClose,
}: Readonly<{ review: HumanReviewDto; onClose: () => void }>) {
  const qc = useQueryClient()
  const [action, setAction] = useState('APPROVE')
  const [reviewerNote, setReviewerNote] = useState('')
  const [overrideCategory, setOverrideCategory] = useState('')

  const { mutate, isPending } = useMutation({
    mutationFn: (req: HumanReviewDecisionRequest) => reviewApi.decide(review.id, req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reviews-pending'] })
      onClose()
    },
  })

  const handleSubmit = (e: React.SyntheticEvent<HTMLFormElement>) => {
    e.preventDefault()
    mutate({
      action,
      reviewerId: 'human-reviewer',
      reviewerNote: reviewerNote || undefined,
      overrideCategory: action === 'APPROVE_WITH_CORRECTIONS' ? overrideCategory : undefined,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="mt-4 border-t pt-4 space-y-3">
      <div>
        <label htmlFor="decision-select" className="text-xs font-medium text-gray-700 block mb-1">
          Decision
        </label>
        <select
          id="decision-select"
          title="Select your decision"
          value={action}
          onChange={e => setAction(e.target.value)}
          className="w-full text-sm border rounded px-2 py-1.5"
        >
          <option value="APPROVE">Approve recommendation</option>
          <option value="REJECT">Reject — keep current category</option>
          <option value="APPROVE_WITH_CORRECTIONS">Override with custom category</option>
        </select>
      </div>

      {action === 'APPROVE_WITH_CORRECTIONS' && (
        <div>
          <label htmlFor="override-category" className="text-xs font-medium text-gray-700 block mb-1">
            Override Category
          </label>
          <input
            id="override-category"
            type="text"
            value={overrideCategory}
            onChange={e => setOverrideCategory(e.target.value)}
            placeholder="e.g. SUPPLIER_DISPUTE"
            className="w-full text-sm border rounded px-2 py-1.5"
            required
          />
        </div>
      )}

      <div>
        <label htmlFor="reviewer-note" className="text-xs font-medium text-gray-700 block mb-1">
          Note (optional)
        </label>
        <textarea
          id="reviewer-note"
          value={reviewerNote}
          onChange={e => setReviewerNote(e.target.value)}
          rows={2}
          className="w-full text-sm border rounded px-2 py-1.5 resize-none"
          placeholder="Add a note for the audit trail…"
        />
      </div>

      <div className="flex gap-2 justify-end">
        <button type="button" onClick={onClose}
          className="text-sm px-3 py-1.5 border rounded hover:bg-gray-50">
          Cancel
        </button>
        <button type="submit" disabled={isPending}
          className="text-sm px-3 py-1.5 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {isPending ? 'Saving…' : 'Submit Decision'}
        </button>
      </div>
    </form>
  )
}

function ReviewCard({ review }: Readonly<{ review: HumanReviewDto }>) {
  const [expanded, setExpanded] = useState(false)
  const priorityColor = PRIORITY_COLORS[review.priority] ?? 'bg-gray-100 text-gray-600'

  return (
    <div className="border rounded-lg p-4 bg-white">
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1 flex-wrap">
            <span className={`text-xs font-semibold px-1.5 py-0.5 rounded ${priorityColor}`}>
              {review.priority}
            </span>
            <span className="text-xs text-gray-500">{review.reviewType}</span>
            <span className="text-xs text-gray-400">
              {new Date(review.queuedAt).toLocaleDateString()}
            </span>
          </div>
          <p className="text-sm text-gray-700 font-medium">{review.reason}</p>
          {review.question && (
            <p className="text-xs text-gray-500 mt-1">{review.question}</p>
          )}
        </div>
        <div className="flex-shrink-0 text-xs text-right">
          <div className="font-mono text-gray-600">
            {Math.round(review.agentConfidence * 100)}% conf
          </div>
        </div>
      </div>

      {review.recommendation && (
        <div className="mt-2 text-xs bg-blue-50 text-blue-700 rounded px-2 py-1.5">
          <span className="font-medium">Recommendation: </span>{review.recommendation}
        </div>
      )}

      <button
        type="button"
        onClick={() => setExpanded(v => !v)}
        className="mt-3 text-xs text-blue-600 hover:underline"
      >
        {expanded ? 'Hide decision form' : 'Make a decision'}
      </button>

      {expanded && <ReviewDecideForm review={review} onClose={() => setExpanded(false)} />}
    </div>
  )
}

export function HumanReviewQueuePage() {
  const { data: queue, isLoading } = useQuery({
    queryKey: ['reviews-pending'],
    queryFn: () => reviewApi.getPending(),
    refetchInterval: 15000,
  })

  const sorted = [...(queue?.reviews ?? [])].sort(
    (a, b) =>
      (PRIORITY_ORDER[a.priority] ?? 99) - (PRIORITY_ORDER[b.priority] ?? 99) ||
      new Date(a.queuedAt).getTime() - new Date(b.queuedAt).getTime(),
  )

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-bold text-gray-900">Human Review Queue</h1>
        {queue && (
          <div className="flex gap-2 text-xs">
            <span className="bg-gray-100 text-gray-700 px-2 py-1 rounded-full">
              {queue.totalPending} pending
            </span>
            {queue.urgentCount > 0 && (
              <span className="bg-red-100 text-red-700 px-2 py-1 rounded-full font-medium">
                {queue.urgentCount} urgent
              </span>
            )}
          </div>
        )}
      </div>

      {isLoading && (
        <div className="text-sm text-gray-500 text-center py-12 animate-pulse">
          Loading reviews…
        </div>
      )}

      {!isLoading && sorted.length === 0 && (
        <div className="text-center py-12 text-gray-400">
          <p className="text-4xl mb-3">✓</p>
          <p className="text-sm">No pending reviews — all caught up!</p>
        </div>
      )}

      <div className="space-y-3">
        {sorted.map(r => <ReviewCard key={r.id} review={r} />)}
      </div>
    </div>
  )
}
