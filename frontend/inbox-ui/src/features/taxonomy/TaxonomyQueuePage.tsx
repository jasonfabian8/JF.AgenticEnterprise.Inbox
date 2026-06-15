import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { taxonomyApi, type TaxonomyProposalDto, type TaxonomyDecisionRequest } from '@/lib/api/client'

function ProposalDecideForm({
  proposal,
  onClose,
}: Readonly<{ proposal: TaxonomyProposalDto; onClose: () => void }>) {
  const qc = useQueryClient()
  const [decision, setDecision] = useState('APPROVED')
  const [decidedBy, setDecidedBy] = useState('')
  const [decisionNote, setDecisionNote] = useState('')

  const { mutate, isPending } = useMutation({
    mutationFn: (req: TaxonomyDecisionRequest) => taxonomyApi.decide(proposal.id, req),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['taxonomy-pending'] })
      onClose()
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!decidedBy.trim()) return
    mutate({ decision, decidedBy, decisionNote: decisionNote || undefined })
  }

  return (
    <form onSubmit={handleSubmit} className="mt-4 border-t pt-4 space-y-3">
      <div>
        <label className="text-xs font-medium text-gray-700 block mb-1">Decision</label>
        <select
          value={decision}
          onChange={e => setDecision(e.target.value)}
          className="w-full text-sm border rounded px-2 py-1.5"
        >
          <option value="APPROVED">Approve — add to taxonomy</option>
          <option value="REJECTED">Reject — discard proposal</option>
        </select>
      </div>

      <div>
        <label className="text-xs font-medium text-gray-700 block mb-1">Your name *</label>
        <input
          type="text"
          value={decidedBy}
          onChange={e => setDecidedBy(e.target.value)}
          placeholder="e.g. jane.doe"
          className="w-full text-sm border rounded px-2 py-1.5"
          required
        />
      </div>

      <div>
        <label className="text-xs font-medium text-gray-700 block mb-1">Note (optional)</label>
        <textarea
          value={decisionNote}
          onChange={e => setDecisionNote(e.target.value)}
          rows={2}
          className="w-full text-sm border rounded px-2 py-1.5 resize-none"
          placeholder="Reason for approval or rejection…"
        />
      </div>

      <div className="flex gap-2 justify-end">
        <button type="button" onClick={onClose}
          className="text-sm px-3 py-1.5 border rounded hover:bg-gray-50">
          Cancel
        </button>
        <button type="submit" disabled={isPending || !decidedBy.trim()}
          className="text-sm px-3 py-1.5 bg-purple-600 text-white rounded hover:bg-purple-700 disabled:opacity-50">
          {isPending ? 'Saving…' : 'Submit'}
        </button>
      </div>
    </form>
  )
}

function ProposalCard({ proposal }: Readonly<{ proposal: TaxonomyProposalDto }>) {
  const [expanded, setExpanded] = useState(false)
  const pct = Math.round(proposal.confidence * 100)
  const barColor = pct >= 85 ? 'bg-green-500' : pct >= 70 ? 'bg-yellow-500' : 'bg-red-500'

  return (
    <div className="border rounded-lg p-4 bg-white">
      <div className="flex items-start justify-between gap-2 mb-2">
        <div>
          <p className="font-medium text-gray-800">{proposal.suggestedLabel}</p>
          <p className="text-xs text-gray-500 mt-0.5">
            Routing → <span className="font-mono">{proposal.suggestedRouting}</span>
            {' · '}suggested by {proposal.createdByAgent}
          </p>
        </div>
        <span className="text-xs text-gray-400 whitespace-nowrap">
          {new Date(proposal.createdAt).toLocaleDateString()}
        </span>
      </div>

      <div className="flex items-center gap-2 mb-3">
        <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden">
          <div className={`h-full ${barColor} rounded-full`} style={{ width: `${pct}%` }} />
        </div>
        <span className="text-xs font-mono w-10 text-right">{pct}%</span>
        <span className="text-xs text-gray-500">{proposal.sampleCount} sample{proposal.sampleCount !== 1 ? 's' : ''}</span>
      </div>

      <button
        onClick={() => setExpanded(v => !v)}
        className="text-xs text-purple-600 hover:underline"
      >
        {expanded ? 'Hide decision form' : 'Make a decision'}
      </button>

      {expanded && <ProposalDecideForm proposal={proposal} onClose={() => setExpanded(false)} />}
    </div>
  )
}

export function TaxonomyQueuePage() {
  const { data: queue, isLoading } = useQuery({
    queryKey: ['taxonomy-pending'],
    queryFn: () => taxonomyApi.getPending(),
    refetchInterval: 30000,
  })

  const sorted = [...(queue?.proposals ?? [])].sort(
    (a, b) => b.confidence - a.confidence,
  )

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-bold text-gray-900">Taxonomy Proposals</h1>
        {queue && (
          <span className="text-xs bg-purple-100 text-purple-700 px-2 py-1 rounded-full">
            {queue.totalPending} pending
          </span>
        )}
      </div>

      {isLoading && (
        <div className="text-sm text-gray-500 text-center py-12 animate-pulse">
          Loading proposals…
        </div>
      )}

      {!isLoading && sorted.length === 0 && (
        <div className="text-center py-12 text-gray-400">
          <p className="text-4xl mb-3">🏷️</p>
          <p className="text-sm">No pending taxonomy proposals.</p>
        </div>
      )}

      <div className="space-y-3">
        {sorted.map(p => <ProposalCard key={p.id} proposal={p} />)}
      </div>
    </div>
  )
}
