import { useQuery } from '@tanstack/react-query'
import { emailApi } from '@/lib/api/client'
import { cn } from '@/lib/utils'

const STEP_DOT: Record<string, string> = {
  COMPLETED: 'bg-green-500',
  RUNNING: 'bg-blue-500 animate-pulse',
  FAILED: 'bg-red-500',
  PENDING: 'bg-gray-300',
  SKIPPED: 'bg-gray-200',
}

function fmtMs(ms: number) {
  return ms < 1000 ? `${ms}ms` : `${(ms / 1000).toFixed(1)}s`
}

export function WorkflowTimeline({ emailId }: { emailId: string }) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['workflow', emailId],
    queryFn: () => emailApi.getWorkflow(emailId),
    retry: false,
  })

  if (isLoading) return <p className="text-xs text-gray-400 py-2">Loading…</p>

  if (isError || !data) {
    return (
      <p className="text-xs text-gray-400 italic py-2">
        No workflow started yet — will appear after the AI pipeline runs in Sprint 1.
      </p>
    )
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between text-xs text-gray-400">
        <span>
          Status: <strong className="text-gray-700">{data.status}</strong>
        </span>
        {data.outcomeType && (
          <span>
            Outcome: <strong className="text-gray-700">{data.outcomeType}</strong>
          </span>
        )}
      </div>

      <ol className="relative space-y-5 border-l-2 border-gray-100 pl-6">
        {data.steps.map(step => {
          const exec = data.agentExecutions.find(a => a.agentType === step.agentType)
          return (
            <li key={step.id} className="relative">
              <span
                className={cn(
                  'absolute -left-[23px] mt-0.5 h-3 w-3 rounded-full border-2 border-white',
                  STEP_DOT[step.status] ?? 'bg-gray-300',
                )}
              />
              <div className="flex items-start justify-between gap-4">
                <div className="min-w-0">
                  <p className="text-sm font-medium text-gray-800">{step.stepName}</p>
                  <p className="text-xs text-gray-400">{step.agentType}</p>
                  {step.outputSummary && (
                    <p className="mt-1 text-xs text-gray-500">{step.outputSummary}</p>
                  )}
                </div>
                <div className="shrink-0 text-right text-xs text-gray-400">
                  <p>{step.status}</p>
                  {step.durationMs > 0 && <p>{fmtMs(step.durationMs)}</p>}
                  {exec?.confidenceScore != null && (
                    <p className="text-blue-500">{Math.round(exec.confidenceScore * 100)}% conf</p>
                  )}
                </div>
              </div>
            </li>
          )
        })}
      </ol>
    </div>
  )
}
