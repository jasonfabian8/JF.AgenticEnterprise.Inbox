import { NavLink, Outlet } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { cn } from '@/lib/utils'
import { useAgentEvents } from '@/lib/signalr/AgentEventContext'
import { reviewApi, taxonomyApi } from '@/lib/api/client'

const NAV = [
  { to: '/inbox',     label: 'Inbox',     icon: '📥' },
  { to: '/simulator', label: 'Simulator', icon: '✉️' },
  { to: '/dashboard', label: 'Dashboard', icon: '📊' },
  { to: '/reviews',   label: 'Reviews',   icon: '👤', badgeKey: 'reviews'  },
  { to: '/taxonomy',  label: 'Taxonomy',  icon: '🏷️', badgeKey: 'taxonomy' },
]

export function AppShell() {
  const { isConnected } = useAgentEvents()

  const { data: reviewQueue }   = useQuery({ queryKey: ['reviews-pending'],  queryFn: () => reviewApi.getPending(),   refetchInterval: 30000 })
  const { data: taxonomyQueue } = useQuery({ queryKey: ['taxonomy-pending'], queryFn: () => taxonomyApi.getPending(), refetchInterval: 60000 })

  const badges: Record<string, number> = {
    reviews:  reviewQueue?.totalPending  ?? 0,
    taxonomy: taxonomyQueue?.totalPending ?? 0,
  }

  return (
    <div className="flex h-screen overflow-hidden">
      <aside className="flex w-56 flex-col border-r border-gray-200 bg-white">
        <div className="flex h-14 items-center gap-2 border-b border-gray-200 px-4">
          <span className="text-lg">🤖</span>
          <span className="text-sm font-semibold text-gray-900">AE Inbox</span>
        </div>

        <nav className="flex-1 space-y-0.5 p-2">
          {NAV.map(item => {
            const count = item.badgeKey ? badges[item.badgeKey] : 0
            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2.5 rounded-md px-3 py-2 text-sm transition-colors',
                    isActive
                      ? 'bg-blue-50 text-blue-700 font-medium'
                      : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900',
                  )
                }
              >
                <span>{item.icon}</span>
                <span className="flex-1">{item.label}</span>
                {count > 0 && (
                  <span className="ml-auto text-xs bg-red-500 text-white rounded-full px-1.5 py-0.5 leading-none font-medium">
                    {count}
                  </span>
                )}
              </NavLink>
            )
          })}
        </nav>

        <div className="border-t border-gray-200 px-4 py-3">
          <div className="flex items-center gap-2 text-xs text-gray-400">
            <span
              className={cn(
                'h-1.5 w-1.5 rounded-full',
                isConnected ? 'bg-green-400' : 'bg-gray-300',
              )}
            />
            {isConnected ? 'Live' : 'Offline'}
          </div>
        </div>
      </aside>

      <main className="flex flex-1 flex-col overflow-auto bg-gray-50">
        <Outlet />
      </main>
    </div>
  )
}
