import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { emailApi } from '@/lib/api/client'
import { StatusBadge, CategoryBadge } from '@/components/ui/badge'
import { WorkflowGraph } from '@/features/workflow/WorkflowGraph'
import { WorkflowStatusCard } from '@/features/workflow/WorkflowStatusCard'
import { AgentActivityPanel } from '@/features/workflow/AgentActivityPanel'
import { InvoiceView } from '@/features/workflow/InvoiceView'
import { ContractView } from '@/features/workflow/ContractView'
import { WorkflowTimeline } from '@/features/workflow/WorkflowTimeline'
import { AgentCollaborationView } from '@/features/workflow/AgentCollaborationView'
import { WorkflowKnowledgeView } from '@/features/workflow/WorkflowKnowledgeView'
import { ReasoningTimeline } from '@/features/workflow/ReasoningTimeline'
import { useAgentEvents } from '@/lib/signalr/AgentEventContext'
import { useEffect } from 'react'

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    weekday: 'short', month: 'short', day: 'numeric',
    year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

function Section({ title, children }: Readonly<{ title: string; children: ReactNode }>) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5">
      <h2 className="mb-4 text-xs font-semibold uppercase tracking-wide text-gray-400">{title}</h2>
      {children}
    </div>
  )
}

function Row({ label, value }: Readonly<{ label: string; value: ReactNode }>) {
  return (
    <div className="flex items-start py-1.5 text-sm">
      <span className="w-36 shrink-0 text-gray-400">{label}</span>
      <span className="text-gray-800">{value}</span>
    </div>
  )
}

const ACTIVE_STATUSES = new Set(['PROCESSING', 'AWAITING_REVIEW', 'ESCALATED', 'UNDER_REVIEW'])

type WorkflowTab = 'graph' | 'timeline'

export function EmailDetailPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()
  const { onWorkflowUpdated, onWorkflowCompleted, joinWorkflow, leaveWorkflow } = useAgentEvents()
  const [activeTab, setActiveTab] = useState<WorkflowTab>('graph')

  const { data: email, isLoading, isError } = useQuery({
    queryKey: ['email', id],
    queryFn: () => emailApi.get(id!),
    enabled: !!id,
  })

  const { data: workflow } = useQuery({
    queryKey: ['workflow', id],
    queryFn: () => emailApi.getWorkflow(id!),
    enabled: !!id,
    retry: false,
    // Poll while the workflow is actively processing
    refetchInterval: (q) => {
      const status = (q.state.data as { status?: string } | undefined)?.status?.toUpperCase()
      return status && ACTIVE_STATUSES.has(status) ? 3000 : false
    },
  })

  // Join the SignalR group for this workflow and invalidate queries on events
  useEffect(() => {
    if (!workflow?.workflowId) return
    const wfId = workflow.workflowId
    joinWorkflow(wfId)

    const invalidateAll = () => {
      queryClient.invalidateQueries({ queryKey: ['email', id] })
      queryClient.invalidateQueries({ queryKey: ['workflow', id] })
      queryClient.invalidateQueries({ queryKey: ['workflow-knowledge', id] })
      queryClient.invalidateQueries({ queryKey: ['workflow-timeline', id] })
    }

    const offUpdated   = onWorkflowUpdated(invalidateAll)
    const offCompleted = onWorkflowCompleted(invalidateAll)

    return () => {
      offUpdated()
      offCompleted()
      leaveWorkflow(wfId)
    }
  }, [workflow?.workflowId, id, joinWorkflow, leaveWorkflow, onWorkflowUpdated, onWorkflowCompleted, queryClient])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24 text-sm text-gray-400">Loading…</div>
    )
  }

  if (isError || !email) {
    return (
      <div className="flex items-center justify-center py-24 text-sm text-red-500">
        Email not found.
      </div>
    )
  }

  const invoiceAnalysis = workflow?.workflowResult?.invoiceAnalysis ?? email.invoiceAnalysis
  const contractAnalysis = workflow?.workflowResult?.contractAnalysis ?? email.contractAnalysis

  return (
    <div className="flex h-full flex-col">
      {/* Breadcrumb toolbar */}
      <div className="flex items-center gap-2 border-b border-gray-200 bg-white px-6 py-3">
        <Link to="/inbox" className="text-sm text-gray-400 hover:text-gray-700">
          ← Inbox
        </Link>
        <span className="text-gray-200">/</span>
        <span className="max-w-sm truncate text-sm text-gray-500">
          {email.subject || '(no subject)'}
        </span>
      </div>

      {/* Scrollable content */}
      <div className="flex-1 overflow-auto p-6 space-y-4">

        {/* Header */}
        <div className="rounded-lg border border-gray-200 bg-white p-5">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0 flex-1">
              <h1 className="truncate text-lg font-semibold text-gray-900">
                {email.subject || '(no subject)'}
              </h1>
              <p className="mt-1 text-sm text-gray-500">
                <span className="font-medium text-gray-700">
                  {email.senderName || email.senderEmail}
                </span>
                {email.senderName && (
                  <span className="ml-1 text-gray-400">&lt;{email.senderEmail}&gt;</span>
                )}
              </p>
              <p className="mt-0.5 text-xs text-gray-400">{fmtDate(email.receivedAt)}</p>
            </div>
            <div className="flex shrink-0 flex-col items-end gap-2">
              <StatusBadge status={email.status} />
              {email.classification?.categoryType && (
                <CategoryBadge category={email.classification.categoryType} />
              )}
            </div>
          </div>
          {(email.processingDurationMs > 0 || email.humanReviewed || email.hasConflict) && (
            <p className="mt-3 text-xs text-gray-400">
              {email.processingDurationMs > 0 && `Processed in ${(email.processingDurationMs / 1000).toFixed(2)}s`}
              {email.humanReviewed && ' · Human reviewed'}
              {email.hasConflict && ' · ⚠ Conflict detected'}
            </p>
          )}
        </div>

        {/* ── Workflow section ────────────────────────────────────────────── */}
        {workflow ? (
          <>
            <WorkflowStatusCard workflow={workflow} />

            {/* Tabbed: Workflow Graph | Reasoning Timeline */}
            <div className="rounded-lg border border-gray-200 bg-white overflow-hidden">
              {/* Tab bar */}
              <div className="flex border-b border-gray-200">
                <button
                  type="button"
                  onClick={() => setActiveTab('graph')}
                  className={`px-5 py-3 text-sm font-medium transition-colors ${
                    activeTab === 'graph'
                      ? 'border-b-2 border-blue-500 text-blue-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  Workflow Graph
                </button>
                <button
                  type="button"
                  onClick={() => setActiveTab('timeline')}
                  className={`px-5 py-3 text-sm font-medium transition-colors ${
                    activeTab === 'timeline'
                      ? 'border-b-2 border-blue-500 text-blue-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  Reasoning Timeline
                </button>
              </div>

              {/* Tab content */}
              <div className="p-5">
                {activeTab === 'graph' ? (
                  <WorkflowGraph
                    workflow={workflow}
                    emailSubject={email.subject || '(no subject)'}
                  />
                ) : (
                  <ReasoningTimeline emailId={email.id} />
                )}
              </div>
            </div>

            <Section title="Agent Activity">
              <AgentActivityPanel
                workflowId={workflow.workflowId}
                emailId={email.id}
              />
            </Section>

            <Section title="Document Understanding">
              <WorkflowKnowledgeView emailId={email.id} />
            </Section>

            <Section title="Agent Conflicts">
              <AgentCollaborationView emailId={email.id} />
            </Section>
          </>
        ) : (
          <Section title="Workflow Timeline">
            <WorkflowTimeline emailId={email.id} />
          </Section>
        )}

        {/* ── Sprint 2 analysis results ───────────────────────────────────── */}
        {invoiceAnalysis && <InvoiceView analysis={invoiceAnalysis} />}
        {contractAnalysis && <ContractView analysis={contractAnalysis} />}

        {/* ── Body ────────────────────────────────────────────────────────── */}
        <Section title="Body">
          {email.bodyPlainText
            ? (
              <pre className="whitespace-pre-wrap font-sans text-sm leading-relaxed text-gray-700">
                {email.bodyPlainText}
              </pre>
            )
            : <span className="text-xs italic text-gray-300">No plain text body</span>
          }
        </Section>

        {/* Attachments */}
        {email.attachments.length > 0 && (
          <Section title={`Attachments (${email.attachments.length})`}>
            <ul className="divide-y divide-gray-100">
              {email.attachments.map(att => (
                <li key={att.id} className="flex items-center justify-between py-2 text-sm">
                  <div>
                    <p className="font-medium text-gray-700">{att.filename}</p>
                    <p className="text-xs text-gray-400">{att.mimeType}</p>
                  </div>
                  <div className="text-right text-xs text-gray-400">
                    <p>{(att.sizeBytes / 1024).toFixed(1)} KB</p>
                    {att.documentType && (
                      <p className="text-blue-500">{att.documentType}</p>
                    )}
                  </div>
                </li>
              ))}
            </ul>
          </Section>
        )}

        {/* Classification */}
        {email.classification && (
          <Section title="Classification">
            <Row label="Category" value={<CategoryBadge category={email.classification.categoryType} />} />
            <Row label="Confidence" value={`${Math.round(email.classification.confidence * 100)}%`} />
            <Row label="Source" value={email.classification.source} />
            {email.classification.isOverridden && (
              <Row label="Overridden" value="Yes (human override)" />
            )}
            {email.classification.reasoning && (
              <div className="mt-3 rounded bg-gray-50 p-3 text-xs text-gray-600 leading-relaxed">
                {email.classification.reasoning}
              </div>
            )}
          </Section>
        )}

        {/* Sprint 1 invoice extraction (legacy) */}
        {email.invoiceExtraction && (
          <Section title="Invoice Data (Legacy)">
            <Row label="Vendor" value={email.invoiceExtraction.vendorName ?? '—'} />
            <Row label="Invoice #" value={email.invoiceExtraction.invoiceNumber ?? '—'} />
            <Row label="Invoice Date" value={email.invoiceExtraction.invoiceDate ?? '—'} />
            <Row label="Due Date" value={email.invoiceExtraction.dueDate ?? '—'} />
            <Row
              label="Total"
              value={
                email.invoiceExtraction.totalAmount == null
                  ? '—'
                  : `${email.invoiceExtraction.currency ?? ''} ${email.invoiceExtraction.totalAmount.toFixed(2)}`
              }
            />
            <Row label="Confidence" value={`${Math.round(email.invoiceExtraction.overallConfidence * 100)}%`} />
          </Section>
        )}

        {/* Sprint 1 contract extraction (legacy) */}
        {email.contractExtraction && (
          <Section title="Contract Data (Legacy)">
            <Row label="Party A" value={email.contractExtraction.partyA ?? '—'} />
            <Row label="Party B" value={email.contractExtraction.partyB ?? '—'} />
            <Row label="Type" value={email.contractExtraction.agreementType ?? '—'} />
            <Row label="Effective" value={email.contractExtraction.effectiveDate ?? '—'} />
            <Row label="Expires" value={email.contractExtraction.expiryDate ?? '—'} />
            <Row label="Confidence" value={`${Math.round(email.contractExtraction.overallConfidence * 100)}%`} />
            {email.contractExtraction.riskFlags.length > 0 && (
              <div className="mt-4">
                <p className="mb-2 text-xs font-medium text-gray-400 uppercase tracking-wide">Risk Flags</p>
                <ul className="space-y-1.5">
                  {email.contractExtraction.riskFlags.map((f) => {
                    const SEVERITY_CLASS: Record<string, string> = {
                      HIGH:   'font-semibold text-red-500',
                      MEDIUM: 'font-semibold text-amber-500',
                    }
                    const severityClass = SEVERITY_CLASS[f.severity] ?? 'font-semibold text-gray-400'
                    return (
                    <li key={`${f.flagType}-${f.severity}`} className="flex items-start gap-2 text-xs">
                      <span className={severityClass}>
                        {f.severity}
                      </span>
                      <span className="text-gray-600">{f.flagType}: {f.excerpt}</span>
                    </li>
                  )})}
                </ul>
              </div>
            )}
          </Section>
        )}

      </div>
    </div>
  )
}
