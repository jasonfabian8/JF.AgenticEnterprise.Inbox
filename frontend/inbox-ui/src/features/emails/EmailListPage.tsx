import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { emailApi } from '@/lib/api/client'
import { StatusBadge, CategoryBadge } from '@/components/ui/badge'

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: 'QUEUED', label: 'Queued' },
  { value: 'PROCESSING', label: 'Processing' },
  { value: 'AWAITING_REVIEW', label: 'Awaiting Review' },
  { value: 'COMPLETED_AUTO', label: 'Completed (Auto)' },
  { value: 'COMPLETED_HUMAN', label: 'Completed (Human)' },
  { value: 'FAILED', label: 'Failed' },
]

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

export function EmailListPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['emails', page, status],
    queryFn: () => emailApi.list(page, 20, status || undefined),
  })

  const totalPages = data ? Math.ceil(data.total / data.pageSize) : 1

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
        <div>
          <h1 className="text-base font-semibold text-gray-900">Inbox</h1>
          {data && (
            <p className="mt-0.5 text-xs text-gray-400">{data.total} emails</p>
          )}
        </div>
        <select
          aria-label="Filter by status"
          value={status}
          onChange={e => { setStatus(e.target.value); setPage(1) }}
          className="rounded-md border border-gray-200 bg-white px-3 py-1.5 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          {STATUS_OPTIONS.map(o => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </div>

      {/* Body */}
      <div className="flex-1 overflow-auto">
        {isLoading && (
          <div className="flex items-center justify-center py-24 text-sm text-gray-400">
            Loading…
          </div>
        )}
        {isError && (
          <div className="flex items-center justify-center py-24 text-sm text-red-500">
            Failed to load. Is the backend running on :5000?
          </div>
        )}
        {data?.items.length === 0 && (
          <div className="flex flex-col items-center justify-center py-24 text-sm text-gray-400 gap-1">
            <span>No emails yet.</span>
            <span className="font-mono text-xs">POST /api/v1/emails/ingest</span>
          </div>
        )}
        {data && data.items.length > 0 && (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 bg-gray-50 text-left text-xs font-medium uppercase tracking-wide text-gray-400">
                <th className="px-6 py-3">Sender</th>
                <th className="px-4 py-3">Subject</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Category</th>
                <th className="px-4 py-3">Received</th>
                <th className="px-4 py-3 text-center">Att.</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 bg-white">
              {data.items.map(email => (
                <tr key={email.id} className="cursor-pointer transition-colors hover:bg-blue-50">
                  <td className="px-6 py-3">
                    <Link to={`/inbox/${email.id}`} className="block">
                      <p className="max-w-[160px] truncate font-medium text-gray-900">
                        {email.senderName || email.senderEmail}
                      </p>
                      <p className="max-w-[160px] truncate text-xs text-gray-400">
                        {email.senderEmail}
                      </p>
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/inbox/${email.id}`} className="block max-w-[280px] truncate text-gray-700">
                      {email.subject || '(no subject)'}
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/inbox/${email.id}`} className="block">
                      <StatusBadge status={email.status} />
                    </Link>
                  </td>
                  <td className="px-4 py-3">
                    <Link to={`/inbox/${email.id}`} className="block">
                      {email.categoryType
                        ? <CategoryBadge category={email.categoryType} />
                        : <span className="text-gray-300">—</span>}
                    </Link>
                  </td>
                  <td className="whitespace-nowrap px-4 py-3 text-gray-500">
                    <Link to={`/inbox/${email.id}`} className="block">
                      {fmtDate(email.receivedAt)}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-center text-gray-400">
                    <Link to={`/inbox/${email.id}`} className="block">
                      {email.attachmentCount > 0
                        ? <span className="font-medium text-gray-600">{email.attachmentCount}</span>
                        : '—'}
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {data && totalPages > 1 && (
        <div className="flex items-center justify-between border-t border-gray-200 bg-white px-6 py-3">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded px-3 py-1 text-sm text-gray-600 hover:bg-gray-100 disabled:text-gray-300"
          >
            Previous
          </button>
          <span className="text-sm text-gray-400">Page {page} of {totalPages}</span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="rounded px-3 py-1 text-sm text-gray-600 hover:bg-gray-100 disabled:text-gray-300"
          >
            Next
          </button>
        </div>
      )}
    </div>
  )
}
