import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { emailApi, type AttachmentIngestDto } from '@/lib/api/client'
import { cn } from '@/lib/utils'

// ── Sample templates ──────────────────────────────────────────────────────────

interface EmailTemplate {
  senderName: string
  senderEmail: string
  subject: string
  body: string
  attachments: AttachmentIngestDto[]
}

const TEMPLATES: Record<string, EmailTemplate> = {
  Invoice: {
    senderName: 'Accounts Payable — Global Supplier Inc.',
    senderEmail: 'ap@globalsupplier.com',
    subject: 'Invoice #GS-2024-00847 — Consulting Services — March 2024',
    body: `Dear Finance Team,

Please find attached the invoice for consulting services rendered during March 2024.

Invoice Details:
  Invoice Number:   GS-2024-00847
  Issue Date:       03/15/2024
  Due Date:         04/15/2024
  Vendor:           Global Supplier Inc.
  Description:      Strategic consulting — 120 hours
  Subtotal:         USD 18,000.00
  Tax (16%):        USD 2,880.00
  Total Due:        USD 20,880.00

Wire Transfer Details:
  Bank:             First National Bank
  Account Number:   ****7890
  Routing Number:   021000021
  Reference:        GS-2024-00847

Please do not hesitate to reach out with any questions.

Best regards,
Accounts Receivable
Global Supplier Inc.`,
    attachments: [
      { filename: 'Invoice_GS-2024-00847.pdf', mimeType: 'application/pdf', sizeBytes: 245_760 },
      { filename: 'Hours_Breakdown_March2024.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', sizeBytes: 38_912 },
    ],
  },

  Contract: {
    senderName: 'Legal — Nexo International Corp.',
    senderEmail: 'legal@nexointernational.com',
    subject: 'Managed Services Agreement — For Review and Signature',
    body: `Dear Team,

Following our recent negotiations, please find attached the Managed IT Services Agreement for your review and signature.

Key Contract Terms:
  Agreement Type:     Managed Services Agreement (MSA)
  Parties:            Nexo International Corp. and your organization
  Term:               04/01/2024 — 03/31/2026 (24 months)
  Auto-Renewal:       Yes, with 60-day non-renewal notice
  Annual Value:       USD 480,000.00
  SLA Commitment:     99.5% monthly uptime
  Penalty Clause:     5% of monthly fee per percentage point below SLA

Key Vendor Obligations:
  - 24/7 support for critical incidents (P1/P2)
  - Monthly capacity and performance reviews
  - Bi-monthly executive reporting
  - Compliance with ISO 27001 and ITIL v4

Please confirm receipt and share your comments by March 25th.

Kind regards,
Legal Department
Nexo International Corp.`,
    attachments: [
      { filename: 'MSA_Nexo_International_v3.pdf', mimeType: 'application/pdf', sizeBytes: 1_048_576 },
      { filename: 'Exhibit_A_Service_Levels.pdf', mimeType: 'application/pdf', sizeBytes: 204_800 },
      { filename: 'Exhibit_B_Pricing_2024.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', sizeBytes: 51_200 },
    ],
  },

  'Commercial Proposal': {
    senderName: 'Corporate Sales — TechSolutions USA',
    senderEmail: 'sales@techsolutions.com',
    subject: 'Commercial Proposal — Cloud Analytics Platform — RFP-2024-112',
    body: `Dear Procurement Team,

In response to your Request for Proposal RFP-2024-112, we are pleased to present our proposal for implementing a cloud analytics platform.

Executive Summary:
  Proposed Solution:  TechAnalytics Cloud Suite Enterprise
  Implementation:     14 weeks
  Licensing:          Annual per-user subscription
  Included Users:     150 licenses
  Annual Price:       USD 135,000.00
  Discount Offered:   15% for a 3-year contract (USD 344,250.00 total)

Our proposal includes:
  ✓ Historical data migration (up to 5 years)
  ✓ Integration with your existing ERP and CRM systems
  ✓ Training for 20 administrator users
  ✓ Premium technical support during the first year
  ✓ Results guarantee: minimum 200% ROI within 18 months

This offer is valid until March 31, 2024.

We would be happy to schedule a live demonstration at your convenience.

Best regards,
Corporate Sales Team
TechSolutions USA`,
    attachments: [
      { filename: 'Proposal_TechAnalytics_RFP-2024-112.pdf', mimeType: 'application/pdf', sizeBytes: 3_145_728 },
      { filename: 'Case_Study_Reference_Client.pdf', mimeType: 'application/pdf', sizeBytes: 819_200 },
    ],
  },

  'Information Request': {
    senderName: 'Mary Johnson',
    senderEmail: 'mjohnson@clientcompany.com',
    subject: 'Follow-up: Case Status and Delivery Timeline',
    body: `Good morning,

I am reaching out to follow up on two pending items from our business relationship:

1. Status of case #CASE-2024-0391:
   According to our records, the case was submitted on February 28th. As of today we have not received a confirmation or assigned reference number. Could you confirm whether it is being processed?

2. Delivery timeline for pending order:
   Under purchase order PO-44821 we ordered 500 units of SKU MON-27-4K with a committed delivery for the second week of March. We need to know if there will be any delay, as we have a production line waiting on this component.

3. Required documentation:
   For Q1 accounting close we need the quality certificates for the last three shipments (January, February, and March).

Thank you for your prompt attention to these items.

Kind regards,
Mary Johnson
Purchasing Manager
Client Company Inc.
Tel: +1 (555) 234-5678`,
    attachments: [],
  },

  Marketing: {
    senderName: 'CloudWorld Summit 2024 Team',
    senderEmail: 'noreply@cloudworldsummit.com',
    subject: '🚀 CloudWorld Summit 2024 — Last Spots Available | 30% Discount',
    body: `Hi there,

The most important cloud technology event of the year is coming and you DON'T want to miss it.

☁️ CLOUDWORLD SUMMIT 2024
📅 Date: April 18–20, 2024
📍 Venue: Convention Center, San Francisco, CA

WHY ATTEND?
  • 80+ technical sessions and keynotes
  • Live demos of the latest AI and Cloud trends
  • Networking with 5,000+ IT professionals
  • Express certifications in AWS, Azure and Google Cloud
  • Expo with 120+ technology vendors

SPECIAL OFFER — VALID UNTIL MARCH 31:
  General Admission:  $299 USD → $209 USD (30% OFF)
  VIP Access:         $599 USD → $419 USD (30% OFF)
  Discount code: CLOUD30

Register your entire team with the corporate package (5+ people) and get an additional 40% off.

[REGISTER NOW]

To unsubscribe from these communications, reply with "UNSUBSCRIBE" in the subject line.

Marketing Team | CloudWorld Summit 2024`,
    attachments: [
      { filename: 'CloudWorld_Summit_2024_Program.pdf', mimeType: 'application/pdf', sizeBytes: 1_572_864 },
    ],
  },

  'Bank Statement': {
    senderName: 'First National Bank — Business Banking',
    senderEmail: 'notifications@firstnationalbank.com',
    subject: 'Business Account Statement — February 2024 — Account ***4521',
    body: `Dear Valued Customer,

Your account statement for February 2024 is now available.

ACCOUNT SUMMARY
  Account Number:     ****-****-****-4521
  Account Type:       Business Checking Account
  Period:             02/01/2024 — 02/29/2024
  Currency:           USD

PERIOD ACTIVITY
  Opening Balance:    $1,245,830.45
  Total Debits:       $  892,150.00
  Total Credits:      $  435,200.00
  Closing Balance:    $  788,880.45

MAIN DEBITS:
  02/04  Batch supplier payment         $  245,000.00
  02/10  Outgoing wire transfer         $  380,000.00
  02/15  Direct debit — services        $   47,150.00
  02/28  Bank fees                      $   15,000.00

MAIN CREDITS:
  02/07  Deposit — ABC Corp             $  200,000.00
  02/14  Incoming wire transfer         $  135,000.00
  02/21  Interest earned                $      200.00

Your full statement is attached in PDF format.
For your security, we will never ask for passwords via email.

Business Banking
First National Bank`,
    attachments: [
      { filename: 'Statement_Feb2024_4521.pdf', mimeType: 'application/pdf', sizeBytes: 614_400 },
    ],
  },

  Unknown: {
    senderName: 'Robert Mendez',
    senderEmail: 'rmendez@example.com',
    subject: 'RE: FWD: Important update',
    body: `Hi,

Following up on the previous email — just wanted to check if you had a chance to review what we sent last week.

Looking forward to your response.

Regards,
Robert

--- Original Message ---
From: Anna Torres
To: Robert Mendez
Subject: FWD: Important update

Forwarding the doc that Marcus sent over.

--- Forwarded Message ---
From: Marcus Ruiz
To: Anna Torres

Anna, please share this with whoever needs it.

[The original file was not included in this forward]`,
    attachments: [],
  },
}

const CATEGORIES = Object.keys(TEMPLATES) as (keyof typeof TEMPLATES)[]

const CATEGORY_STYLES: Record<string, string> = {
  Invoice:               'bg-violet-50 text-violet-700 border-violet-200 hover:bg-violet-100',
  Contract:              'bg-indigo-50 text-indigo-700 border-indigo-200 hover:bg-indigo-100',
  'Commercial Proposal': 'bg-sky-50 text-sky-700 border-sky-200 hover:bg-sky-100',
  'Information Request': 'bg-cyan-50 text-cyan-700 border-cyan-200 hover:bg-cyan-100',
  Marketing:             'bg-pink-50 text-pink-700 border-pink-200 hover:bg-pink-100',
  'Bank Statement':      'bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100',
  Unknown:               'bg-gray-50 text-gray-600 border-gray-200 hover:bg-gray-100',
}

// ── Attachment row ────────────────────────────────────────────────────────────

function AttachmentRow({
  att,
  onChange,
  onRemove,
}: Readonly<{
  att: AttachmentIngestDto
  onChange: (next: AttachmentIngestDto) => void
  onRemove: () => void
}>) {
  return (
    <div className="flex items-center gap-2 rounded-lg border border-gray-200 bg-gray-50 px-3 py-2">
      <span className="text-sm text-gray-400 select-none">📎</span>
      <input
        className="min-w-0 flex-1 bg-transparent text-sm text-gray-700 outline-none placeholder:text-gray-300"
        placeholder="filename.pdf"
        value={att.filename}
        onChange={e => onChange({ ...att, filename: e.target.value })}
      />
      <select
        className="shrink-0 rounded border border-gray-200 bg-white px-1.5 py-0.5 text-xs text-gray-600 outline-none"
        title="File type"
        value={att.mimeType}
        onChange={e => onChange({ ...att, mimeType: e.target.value })}
      >
        <option value="application/pdf">PDF</option>
        <option value="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet">Excel</option>
        <option value="application/vnd.openxmlformats-officedocument.wordprocessingml.document">Word</option>
        <option value="text/plain">TXT</option>
        <option value="text/html">HTML</option>
        <option value="image/png">PNG</option>
        <option value="image/jpeg">JPG</option>
      </select>
      <button
        type="button"
        onClick={onRemove}
        className="ml-1 text-gray-300 hover:text-red-400 transition-colors"
        aria-label="Remove attachment"
      >
        ×
      </button>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

interface FormState {
  senderName: string
  senderEmail: string
  subject: string
  body: string
  attachments: AttachmentIngestDto[]
}

const EMPTY: FormState = {
  senderName: '',
  senderEmail: '',
  subject: '',
  body: '',
  attachments: [],
}

export function SimulatorPage() {
  const [form, setForm] = useState<FormState>(EMPTY)
  const [activeCategory, setActiveCategory] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      emailApi.ingest({
        senderName: form.senderName,
        senderEmail: form.senderEmail,
        subject: form.subject,
        bodyPlainText: form.body,
        attachments: form.attachments.length > 0 ? form.attachments : undefined,
      }),
  })

  function applyTemplate(category: string) {
    const tpl = TEMPLATES[category]
    if (!tpl) return
    setForm({ ...tpl })
    setActiveCategory(category)
    mutation.reset()
  }

  function setField<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm(prev => ({ ...prev, [key]: value }))
    mutation.reset()
  }

  function addAttachment() {
    setField('attachments', [
      ...form.attachments,
      { filename: '', mimeType: 'application/pdf', sizeBytes: 0 },
    ])
  }

  function updateAttachment(i: number, next: AttachmentIngestDto) {
    const updated = [...form.attachments]
    updated[i] = next
    setField('attachments', updated)
  }

  function removeAttachment(i: number) {
    setField('attachments', form.attachments.filter((_, idx) => idx !== i))
  }

  const canSubmit =
    form.senderEmail.trim() !== '' &&
    form.subject.trim() !== '' &&
    form.body.trim() !== '' &&
    !mutation.isPending

  return (
    <div className="flex h-full flex-col">
      {/* Header bar */}
      <div className="flex items-center gap-3 border-b border-gray-200 bg-white px-6 py-3">
        <span className="text-sm font-semibold text-gray-800">Email Simulator</span>
        <span className="text-xs text-gray-400">
          Generate and send test emails to the agent pipeline
        </span>
      </div>

      <div className="flex-1 overflow-auto p-6">
        <div className="mx-auto max-w-2xl space-y-5">

          {/* Category quick-fill */}
          <div className="rounded-lg border border-gray-200 bg-white p-5">
            <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-gray-400">
              Generate by category
            </p>
            <div className="flex flex-wrap gap-2">
              {CATEGORIES.map(cat => (
                <button
                  key={cat}
                  type="button"
                  onClick={() => applyTemplate(cat)}
                  className={cn(
                    'rounded-full border px-3 py-1.5 text-xs font-medium transition-all',
                    activeCategory === cat
                      ? cn(CATEGORY_STYLES[cat], 'ring-2 ring-offset-1 ring-current')
                      : CATEGORY_STYLES[cat],
                  )}
                >
                  {cat}
                </button>
              ))}
            </div>
            {activeCategory && (
              <p className="mt-3 text-xs text-gray-400">
                Template <strong className="text-gray-600">{activeCategory}</strong> loaded — you can edit the fields before sending.
              </p>
            )}
          </div>

          {/* Form */}
          <div className="rounded-lg border border-gray-200 bg-white p-5 space-y-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-gray-400">
              Email data
            </p>

            {/* Sender row */}
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="mb-1 block text-xs text-gray-500">Sender name</label>
                <input
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                  placeholder="Acme Corp — Finance"
                  value={form.senderName}
                  onChange={e => setField('senderName', e.target.value)}
                />
              </div>
              <div>
                <label className="mb-1 block text-xs text-gray-500">
                  Sender email <span className="text-red-400">*</span>
                </label>
                <input
                  type="email"
                  className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                  placeholder="sender@company.com"
                  value={form.senderEmail}
                  onChange={e => setField('senderEmail', e.target.value)}
                />
              </div>
            </div>

            {/* Subject */}
            <div>
              <label className="mb-1 block text-xs text-gray-500">
                Subject <span className="text-red-400">*</span>
              </label>
              <input
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300"
                placeholder="Invoice #001 — January services"
                value={form.subject}
                onChange={e => setField('subject', e.target.value)}
              />
            </div>

            {/* Body */}
            <div>
              <label className="mb-1 block text-xs text-gray-500">
                Body <span className="text-red-400">*</span>
              </label>
              <textarea
                rows={14}
                className="w-full rounded-lg border border-gray-200 px-3 py-2 text-sm text-gray-800 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 placeholder:text-gray-300 resize-y font-mono leading-relaxed"
                placeholder="Write the email body here…"
                value={form.body}
                onChange={e => setField('body', e.target.value)}
              />
            </div>

            {/* Attachments */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="text-xs text-gray-500">Attachments (optional)</label>
                <button
                  type="button"
                  onClick={addAttachment}
                  className="flex items-center gap-1 rounded-md border border-dashed border-gray-300 px-2.5 py-1 text-xs text-gray-400 hover:border-blue-300 hover:text-blue-500 transition-colors"
                >
                  + Add attachment
                </button>
              </div>

              {form.attachments.length > 0 ? (
                <div className="space-y-2">
                  {form.attachments.map((att, i) => (
                    <AttachmentRow
                      key={i}
                      att={att}
                      onChange={next => updateAttachment(i, next)}
                      onRemove={() => removeAttachment(i)}
                    />
                  ))}
                </div>
              ) : (
                <p className="text-xs italic text-gray-300 py-1">No attachments</p>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between gap-4">
            <button
              type="button"
              onClick={() => { setForm(EMPTY); setActiveCategory(null); mutation.reset() }}
              className="text-xs text-gray-400 hover:text-gray-600 transition-colors"
            >
              Clear form
            </button>

            <button
              type="button"
              onClick={() => mutation.mutate()}
              disabled={!canSubmit}
              className={cn(
                'flex items-center gap-2 rounded-lg px-5 py-2.5 text-sm font-medium transition-all',
                canSubmit
                  ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-sm'
                  : 'bg-gray-100 text-gray-400 cursor-not-allowed',
              )}
            >
              {mutation.isPending ? (
                <>
                  <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white border-t-transparent" />
                  Sending…
                </>
              ) : (
                'Send to Inbox →'
              )}
            </button>
          </div>

          {/* Success */}
          {mutation.isSuccess && (
            <div className="rounded-lg border border-green-200 bg-green-50 px-5 py-4">
              <p className="text-sm font-medium text-green-800">
                ✓ Email sent to the pipeline
              </p>
              <p className="mt-1 text-xs text-green-600">
                ID: <span className="font-mono">{mutation.data.emailId}</span>
                {' · '}Initial status: <strong>{mutation.data.status}</strong>
              </p>
              <div className="mt-3 flex gap-3">
                <Link
                  to={`/inbox/${mutation.data.emailId}`}
                  className="text-xs font-medium text-green-700 underline hover:text-green-900"
                >
                  View in Inbox →
                </Link>
                <button
                  type="button"
                  onClick={() => { setForm(EMPTY); setActiveCategory(null); mutation.reset() }}
                  className="text-xs text-green-600 hover:text-green-800"
                >
                  Send another
                </button>
              </div>
            </div>
          )}

          {/* Error */}
          {mutation.isError && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-5 py-4">
              <p className="text-sm font-medium text-red-800">Failed to send email</p>
              <p className="mt-1 text-xs text-red-600">
                {mutation.error instanceof Error
                  ? mutation.error.message
                  : 'Unknown error. Make sure the API is running.'}
              </p>
            </div>
          )}

        </div>
      </div>
    </div>
  )
}
