import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { dashboardApi, type DashboardStats, type AgentStat, type ThroughputDay } from '@/lib/api/client'

// ── Primitive UI helpers ──────────────────────────────────────────────────────

function Card({ children, className = '' }: Readonly<{ children: React.ReactNode; className?: string }>) {
  return (
    <div className={`bg-white rounded-xl border border-gray-200 p-5 ${className}`}>
      {children}
    </div>
  )
}

function SectionTitle({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <p className="text-xs font-semibold uppercase tracking-wide text-gray-400 mb-3">
      {children}
    </p>
  )
}

// ── Stat tile ─────────────────────────────────────────────────────────────────

function StatTile({
  label,
  value,
  sub,
  accent,
}: Readonly<{ label: string; value: React.ReactNode; sub?: string; accent?: string }>) {
  return (
    <Card>
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className={`text-3xl font-bold ${accent ?? 'text-gray-900'}`}>{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </Card>
  )
}

// ── Mini bar chart ────────────────────────────────────────────────────────────

function MiniBar({ value, max, color }: Readonly<{ value: number; max: number; color: string }>) {
  const pct = max > 0 ? Math.round((value / max) * 100) : 0
  return (
    <div className="flex items-center gap-2">
      <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
        <div className={`h-full ${color} rounded-full transition-all`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs font-mono w-8 text-right text-gray-600">{value}</span>
    </div>
  )
}

// ── Throughput sparkline (pure CSS bar chart) ─────────────────────────────────

function ThroughputChart({ days }: Readonly<{ days: ThroughputDay[] }>) {
  const maxVal = Math.max(...days.map(d => d.ingested), 1)

  if (!days.length) {
    return <p className="text-xs text-gray-400 text-center py-4">No activity data</p>
  }

  return (
    <div className="flex items-end gap-1.5 h-24">
      {days.map(d => {
        const ingestedH   = Math.round((d.ingested   / maxVal) * 96)
        const completedH  = Math.round((d.completed  / maxVal) * 96)
        return (
          <div key={d.date} className="flex-1 flex flex-col items-center gap-0.5">
            <div className="relative w-full flex items-end justify-center gap-0.5" style={{ height: 96 }}>
              <div
                className="flex-1 bg-blue-200 rounded-t"
                style={{ height: ingestedH }}
                title={`Ingested: ${d.ingested}`}
              />
              <div
                className="flex-1 bg-blue-500 rounded-t"
                style={{ height: completedH }}
                title={`Completed: ${d.completed}`}
              />
            </div>
            <span className="text-[9px] text-gray-400 rotate-[-35deg] origin-center whitespace-nowrap">
              {d.date.slice(5)}
            </span>
          </div>
        )
      })}
    </div>
  )
}

// ── Confidence band donut (CSS conic-gradient) ────────────────────────────────

function ConfidenceDonut({
  high, medium, low,
}: Readonly<{ high: number; medium: number; low: number }>) {
  const total = high + medium + low
  if (total === 0) return <p className="text-xs text-gray-400 text-center py-4">No data</p>

  const highPct   = Math.round(high   / total * 100)
  const mediumPct = Math.round(medium / total * 100)
  const lowPct    = 100 - highPct - mediumPct

  const gradient = `conic-gradient(
    #22c55e 0% ${highPct}%,
    #eab308 ${highPct}% ${highPct + mediumPct}%,
    #ef4444 ${highPct + mediumPct}% 100%
  )`

  return (
    <div className="flex items-center gap-6">
      <div
        className="w-20 h-20 rounded-full flex-shrink-0"
        style={{ background: gradient }}
      />
      <div className="space-y-1.5 text-xs">
        <div className="flex items-center gap-1.5">
          <span className="w-2.5 h-2.5 rounded-full bg-green-500 inline-block" />
          <span className="text-gray-600">High ≥85%</span>
          <span className="ml-auto font-mono font-semibold">{highPct}%</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="w-2.5 h-2.5 rounded-full bg-yellow-500 inline-block" />
          <span className="text-gray-600">Medium 70–84%</span>
          <span className="ml-auto font-mono font-semibold">{mediumPct}%</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="w-2.5 h-2.5 rounded-full bg-red-500 inline-block" />
          <span className="text-gray-600">Low &lt;70%</span>
          <span className="ml-auto font-mono font-semibold">{lowPct}%</span>
        </div>
      </div>
    </div>
  )
}

// ── Agent stats table ─────────────────────────────────────────────────────────

function AgentTable({ agents }: Readonly<{ agents: AgentStat[] }>) {
  if (!agents.length) {
    return <p className="text-xs text-gray-400 text-center py-4">No agent executions yet</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-xs">
        <thead>
          <tr className="border-b border-gray-100 text-gray-400">
            <th className="text-left py-1.5 pr-3 font-medium">Agent</th>
            <th className="text-right py-1.5 px-2 font-medium">Runs</th>
            <th className="text-right py-1.5 px-2 font-medium">Success</th>
            <th className="text-right py-1.5 px-2 font-medium">Avg ms</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-50">
          {agents.map(a => (
            <tr key={a.agentType}>
              <td className="py-2 pr-3 text-gray-700 font-medium">{a.agentType}</td>
              <td className="py-2 px-2 text-right font-mono text-gray-600">{a.totalRuns}</td>
              <td className="py-2 px-2 text-right">
                <span className={`font-semibold ${
                  a.successRate >= 90 ? 'text-green-600' :
                  a.successRate >= 70 ? 'text-yellow-600' : 'text-red-600'
                }`}>
                  {a.successRate}%
                </span>
              </td>
              <td className="py-2 px-2 text-right font-mono text-gray-500">{a.avgDurationMs.toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// ── Main dashboard ────────────────────────────────────────────────────────────

function DashboardContent({ stats }: Readonly<{ stats: DashboardStats }>) {
  const { emails, classification, agents, conflicts, queues, throughput } = stats
  const maxCat = Math.max(...classification.distribution.map(x => x.count), 1)

  const CONFLICT_COLORS: Record<string, string> = {
    CATEGORY_MISMATCH: 'bg-orange-400',
    LOW_CONFIDENCE:    'bg-yellow-400',
    MISSING_INFORMATION: 'bg-blue-400',
    ROUTING_DISPUTE:   'bg-red-400',
  }

  return (
    <div className="space-y-6">

      {/* ── Top KPIs ──────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
        <StatTile
          label="Total Emails"
          value={emails.total}
          sub="all time"
        />
        <StatTile
          label="Automation Rate"
          value={`${emails.automationRate}%`}
          sub={`${emails.completedAuto} auto-resolved`}
          accent={emails.automationRate >= 70 ? 'text-green-600' : 'text-yellow-600'}
        />
        <StatTile
          label="Pending Reviews"
          value={queues.pendingReviews}
          sub="human review queue"
          accent={queues.pendingReviews > 0 ? 'text-red-600' : 'text-gray-900'}
        />
        <StatTile
          label="Taxonomy Proposals"
          value={queues.pendingTaxonomy}
          sub="awaiting approval"
          accent={queues.pendingTaxonomy > 0 ? 'text-purple-600' : 'text-gray-900'}
        />
      </div>

      {/* ── Email status breakdown + throughput ───────────────────────── */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Card>
          <SectionTitle>Email Status Breakdown</SectionTitle>
          <div className="space-y-2.5">
            {[
              { label: 'Completed (Auto)',  value: emails.completedAuto,  color: 'bg-green-500' },
              { label: 'Completed (Human)', value: emails.completedHuman, color: 'bg-teal-500'  },
              { label: 'Awaiting Review',   value: emails.awaitingReview, color: 'bg-amber-500' },
              { label: 'Processing',        value: emails.processing,     color: 'bg-blue-400'  },
              { label: 'Failed',            value: emails.failed,         color: 'bg-red-500'   },
            ].map(item => (
              <div key={item.label}>
                <div className="flex justify-between text-xs text-gray-600 mb-0.5">
                  <span>{item.label}</span>
                </div>
                <MiniBar value={item.value} max={emails.total} color={item.color} />
              </div>
            ))}
          </div>
          <div className="mt-4">
            <Link to="/inbox" className="text-xs text-blue-600 hover:underline">
              View all emails →
            </Link>
          </div>
        </Card>

        <Card>
          <SectionTitle>7-Day Throughput</SectionTitle>
          <ThroughputChart days={throughput} />
          <div className="flex gap-4 mt-3 text-xs text-gray-400">
            <span className="flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded bg-blue-200 inline-block" />
              Ingested
            </span>
            <span className="flex items-center gap-1">
              <span className="w-2.5 h-2.5 rounded bg-blue-500 inline-block" />
              Completed
            </span>
          </div>
        </Card>
      </div>

      {/* ── Classification ────────────────────────────────────────────── */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Card>
          <SectionTitle>Category Distribution</SectionTitle>
          {classification.distribution.length === 0 ? (
            <p className="text-xs text-gray-400 text-center py-4">No classifications yet</p>
          ) : (
            <div className="space-y-2.5">
              {classification.distribution.slice(0, 8).map(item => (
                <div key={item.category}>
                  <div className="flex justify-between text-xs text-gray-600 mb-0.5">
                    <span className="truncate">{item.category}</span>
                  </div>
                  <MiniBar value={item.count} max={maxCat} color="bg-blue-500" />
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card>
          <SectionTitle>Confidence Distribution</SectionTitle>
          <div className="mb-4">
            <p className="text-3xl font-bold text-gray-900">
              {classification.confidence.average}%
              <span className="text-sm font-normal text-gray-400 ml-1">avg</span>
            </p>
          </div>
          <ConfidenceDonut
            high={classification.confidence.high}
            medium={classification.confidence.medium}
            low={classification.confidence.low}
          />
        </Card>
      </div>

      {/* ── Agent performance + conflicts ─────────────────────────────── */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Card>
          <SectionTitle>Agent Performance</SectionTitle>
          <AgentTable agents={agents} />
        </Card>

        <Card>
          <SectionTitle>Agent Conflicts</SectionTitle>
          <div className="flex gap-4 mb-4">
            <div>
              <p className="text-3xl font-bold text-gray-900">{conflicts.total}</p>
              <p className="text-xs text-gray-400">total detected</p>
            </div>
            <div>
              <p className={`text-3xl font-bold ${conflicts.active > 0 ? 'text-orange-500' : 'text-green-500'}`}>
                {conflicts.active}
              </p>
              <p className="text-xs text-gray-400">unresolved</p>
            </div>
          </div>

          {conflicts.byType.length > 0 ? (
            <div className="space-y-2">
              {conflicts.byType.map(ct => (
                <div key={ct.type} className="flex items-center gap-2 text-xs">
                  <span className={`w-2.5 h-2.5 rounded-full flex-shrink-0 ${CONFLICT_COLORS[ct.type] ?? 'bg-gray-400'}`} />
                  <span className="flex-1 text-gray-600">{ct.type.replace(/_/g, ' ')}</span>
                  <span className="font-mono font-semibold">{ct.count}</span>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-xs text-gray-400">No conflicts detected yet.</p>
          )}
        </Card>
      </div>

      {/* ── Action links ──────────────────────────────────────────────── */}
      {(queues.pendingReviews > 0 || queues.pendingTaxonomy > 0) && (
        <Card className="bg-amber-50 border-amber-200">
          <p className="text-sm font-medium text-amber-800 mb-2">Pending Actions</p>
          <div className="flex gap-4 text-sm">
            {queues.pendingReviews > 0 && (
              <Link to="/reviews" className="text-blue-600 hover:underline">
                {queues.pendingReviews} review{queues.pendingReviews !== 1 ? 's' : ''} waiting →
              </Link>
            )}
            {queues.pendingTaxonomy > 0 && (
              <Link to="/taxonomy" className="text-purple-600 hover:underline">
                {queues.pendingTaxonomy} taxonomy proposal{queues.pendingTaxonomy !== 1 ? 's' : ''} →
              </Link>
            )}
          </div>
        </Card>
      )}

    </div>
  )
}

export function DashboardPage() {
  const { data: stats, isLoading, isError, refetch } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardApi.getStats(),
    refetchInterval: 30000,
  })

  return (
    <div className="flex flex-col h-full">
      <div className="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4">
        <h1 className="text-lg font-bold text-gray-900">Dashboard</h1>
        <button
          onClick={() => refetch()}
          className="text-xs text-gray-400 hover:text-gray-700 border rounded px-2 py-1"
        >
          Refresh
        </button>
      </div>

      <div className="flex-1 overflow-auto p-6">
        {isLoading && (
          <div className="flex items-center justify-center py-24 text-sm text-gray-400 animate-pulse">
            Loading stats…
          </div>
        )}

        {isError && (
          <div className="flex items-center justify-center py-24 text-sm text-red-500">
            Failed to load dashboard stats.
          </div>
        )}

        {stats && <DashboardContent stats={stats} />}
      </div>
    </div>
  )
}
