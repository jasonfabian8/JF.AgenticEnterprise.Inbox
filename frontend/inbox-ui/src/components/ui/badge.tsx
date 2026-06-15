import { cn } from '@/lib/utils'

const STATUS_COLORS: Record<string, string> = {
  QUEUED: 'bg-gray-100 text-gray-700',
  PROCESSING: 'bg-blue-100 text-blue-700',
  AWAITING_REVIEW: 'bg-amber-100 text-amber-700',
  COMPLETED_AUTO: 'bg-green-100 text-green-700',
  COMPLETED_HUMAN: 'bg-teal-100 text-teal-700',
  FAILED: 'bg-red-100 text-red-700',
  REJECTED: 'bg-gray-200 text-gray-600',
}

const CATEGORY_COLORS: Record<string, string> = {
  Invoice: 'bg-violet-100 text-violet-700',
  Contract: 'bg-indigo-100 text-indigo-700',
  Proposal: 'bg-sky-100 text-sky-700',
  'Information Request': 'bg-cyan-100 text-cyan-700',
  Marketing: 'bg-pink-100 text-pink-700',
  'Bank Statement': 'bg-emerald-100 text-emerald-700',
  UNKNOWN: 'bg-gray-100 text-gray-500',
}

export function StatusBadge({ status }: { status: string }) {
  const color = STATUS_COLORS[status] ?? 'bg-gray-100 text-gray-600'
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', color)}>
      {status.replace(/_/g, ' ')}
    </span>
  )
}

export function CategoryBadge({ category }: { category: string }) {
  const color = CATEGORY_COLORS[category] ?? 'bg-gray-100 text-gray-600'
  return (
    <span className={cn('inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium', color)}>
      {category}
    </span>
  )
}
