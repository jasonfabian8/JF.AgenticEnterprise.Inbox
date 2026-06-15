import type { InvoiceAnalysisDto } from '@/lib/api/client'
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
  analysis: InvoiceAnalysisDto
}

export function InvoiceView({ analysis }: Props) {
  const pct = Math.round(analysis.confidence * 100)
  const barColor = pct >= 80 ? 'bg-green-500' : pct >= 50 ? 'bg-amber-400' : 'bg-red-400'

  const totalFormatted =
    analysis.totalAmount != null
      ? [analysis.currency, analysis.totalAmount.toFixed(2)].filter(Boolean).join(' ')
      : null

  return (
    <div className="rounded-lg border border-violet-100 bg-white p-5">
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-gray-400">
          Invoice Analysis
        </h2>
        <span className="inline-flex items-center rounded-full bg-violet-50 px-2 py-0.5 text-xs font-medium text-violet-700">
          {pct}% confidence
        </span>
      </div>

      <Row label="Supplier" value={analysis.supplier} />
      <Row label="Invoice #" value={analysis.invoiceNumber} />
      <Row label="Invoice Date" value={analysis.invoiceDate} />
      <Row label="Due Date" value={analysis.dueDate} />
      <Row label="Total Amount" value={totalFormatted} />

      <div className="mt-3">
        <div className="h-1 w-full rounded-full bg-gray-100">
          <div
            className={cn('h-1 rounded-full transition-all duration-500', barColor)}
            style={{ width: `${pct}%` }}
          />
        </div>
      </div>

      {analysis.summary && (
        <p className="mt-3 text-xs text-gray-500 leading-relaxed bg-gray-50 rounded-md p-2.5">
          {analysis.summary}
        </p>
      )}
    </div>
  )
}
