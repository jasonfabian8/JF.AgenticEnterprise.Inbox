import type { ContractAnalysisDto } from '@/lib/api/client'
import { cn } from '@/lib/utils'

function Row({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div className="flex items-start py-1.5 text-sm">
      <span className="w-36 shrink-0 text-gray-400">{label}</span>
      <span className="text-gray-800">{value ?? '—'}</span>
    </div>
  )
}

interface Props {
  analysis: ContractAnalysisDto
}

export function ContractView({ analysis }: Props) {
  const pct = Math.round(analysis.confidence * 100)
  const barColor = pct >= 80 ? 'bg-green-500' : pct >= 50 ? 'bg-amber-400' : 'bg-red-400'

  return (
    <div className="rounded-lg border border-indigo-100 bg-white p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-gray-400">
          Contract Analysis
        </h2>
        <span className="inline-flex items-center rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">
          {pct}% confidence
        </span>
      </div>

      <Row label="Type" value={analysis.contractType} />
      <Row label="Effective" value={analysis.effectiveDate} />
      <Row label="Expires" value={analysis.expirationDate} />
      <Row label="Renewal Clause" value={analysis.renewalClause} />

      {analysis.parties.length > 0 && (
        <div className="flex items-start py-1.5 text-sm">
          <span className="w-36 shrink-0 text-gray-400">Parties</span>
          <div className="flex flex-wrap gap-1">
            {analysis.parties.map((party, i) => (
              <span
                key={i}
                className="inline-flex items-center rounded-full bg-indigo-50 px-2 py-0.5 text-xs text-indigo-700"
              >
                {party}
              </span>
            ))}
          </div>
        </div>
      )}

      {analysis.keyObligations.length > 0 && (
        <div className="mt-3">
          <p className="text-xs font-medium text-gray-400 mb-1.5">Key Obligations</p>
          <ul className="space-y-1">
            {analysis.keyObligations.map((obligation, i) => (
              <li key={i} className="flex items-start gap-1.5 text-xs text-gray-600">
                <span className="text-gray-300 mt-0.5 select-none">•</span>
                {obligation}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="mt-3">
        <div className="h-1 w-full rounded-full bg-gray-100">
          <div
            className={cn('h-1 rounded-full transition-all duration-500', barColor)}
            style={{ width: `${pct}%` }}
          />
        </div>
      </div>

      {analysis.reasoning && (
        <p className="mt-3 text-xs text-gray-500 leading-relaxed bg-gray-50 rounded-md p-2.5">
          {analysis.reasoning}
        </p>
      )}
    </div>
  )
}
