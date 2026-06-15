import type { WorkflowDetail } from '@/lib/api/client'
import { StatusBadge } from '@/components/ui/badge'

const NEXT_AGENT_LABELS: Record<string, string> = {
  InvoiceAgent: 'Invoice Agent',
  ContractAgent: 'Contract Agent',
  HumanReview: 'Human Review',
  Complete: 'Completed',
}

function fmtDate(iso: string | null) {
  if (!iso) return null
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

interface Props {
  workflow: WorkflowDetail
}

export function WorkflowStatusCard({ workflow }: Props) {
  const decision = workflow.orchestrationDecision
  const result = workflow.workflowResult

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-5 py-3.5 flex flex-wrap items-center gap-x-6 gap-y-2">
      <div>
        <p className="text-xs text-gray-400 mb-1">Status</p>
        <StatusBadge status={workflow.status} />
      </div>

      {decision && (
        <div>
          <p className="text-xs text-gray-400 mb-1">Routed to</p>
          <span className="text-sm font-medium text-gray-700">
            {NEXT_AGENT_LABELS[decision.nextAgent] ?? decision.nextAgent}
          </span>
        </div>
      )}

      {decision && (
        <div className="max-w-xs">
          <p className="text-xs text-gray-400 mb-1">Routing reason</p>
          <p className="text-xs text-gray-600 line-clamp-1">{decision.reasoning}</p>
        </div>
      )}

      {result?.summary && (
        <div className="max-w-xs">
          <p className="text-xs text-gray-400 mb-1">Summary</p>
          <p className="text-xs text-gray-600 line-clamp-1">{result.summary}</p>
        </div>
      )}

      <div className="ml-auto text-right shrink-0">
        <p className="text-xs text-gray-400">Started {fmtDate(workflow.startedAt)}</p>
        {workflow.completedAt && (
          <p className="text-xs text-gray-400 mt-0.5">Completed {fmtDate(workflow.completedAt)}</p>
        )}
      </div>
    </div>
  )
}
