# Section 06 — User Stories

---

## Epic: Email Ingestion

### US-001 — Email Upload via Web UI

**As an** Operations Analyst,
**I want to** upload or paste an email into the system,
**So that** I can trigger automated processing without requiring mailbox integration in the demo.

**Business Value:** Enables controlled demonstration and manual testing of the pipeline.
**Priority:** Must Have
**Dependencies:** None

**Acceptance Criteria:**
- [ ] User can paste email text (headers + body) into a text area and click "Process"
- [ ] User can upload a `.eml` file via drag-and-drop or file picker
- [ ] System confirms ingestion with email ID and initial status
- [ ] Error message displayed for invalid input formats

---

### US-002 — Multi-Attachment Email Processing

**As an** Operations Analyst,
**I want** the system to handle emails with multiple attachments,
**So that** I don't have to split emails before uploading them.

**Business Value:** Real business emails commonly have multiple documents attached.
**Priority:** Should Have
**Dependencies:** US-001, FR-002

**Acceptance Criteria:**
- [ ] System detects and processes up to 5 attachments per email
- [ ] Each attachment is analyzed independently
- [ ] Processing results reference which attachment each finding came from
- [ ] UI shows attachment list with per-attachment status

---

### US-003 — Processing Status Tracking

**As a** Business User,
**I want to** see the real-time processing status of an email I submitted,
**So that** I know whether it has been processed, is in progress, or needs my attention.

**Business Value:** Reduces uncertainty and prevents redundant manual follow-up.
**Priority:** Must Have
**Dependencies:** US-001

**Acceptance Criteria:**
- [ ] Status values: QUEUED, PROCESSING, COMPLETED, AWAITING_REVIEW, FAILED
- [ ] Status visible in email list and email detail views
- [ ] Status updates without requiring page refresh
- [ ] Failed status includes human-readable error reason

---

## Epic: Agent Orchestration

### US-004 — Orchestrated Agent Workflow

**As an** Operations Analyst,
**I want** a single orchestrator to coordinate all agent activity,
**So that** I have a consistent, predictable processing flow I can trust and review.

**Business Value:** Consistency and predictability are prerequisites for user trust in automation.
**Priority:** Must Have
**Dependencies:** FR-004

**Acceptance Criteria:**
- [ ] Every email processing job has exactly one Orchestrator Agent execution record
- [ ] Orchestrator selects and invokes appropriate specialized agents
- [ ] Orchestrator produces a unified result merging all agent outputs
- [ ] Orchestrator execution timeline visible in UI

---

### US-005 — Agent Execution Timeline Visualization

**As an** Operations Analyst,
**I want to** see a visual timeline of which agents ran and in what order,
**So that** I can understand how a processing decision was reached.

**Business Value:** Builds user trust by making the AI reasoning process visible and navigable.
**Priority:** Must Have (for demo)
**Dependencies:** FR-005

**Acceptance Criteria:**
- [ ] Timeline shows each agent as a labeled block with start/end time
- [ ] Color coding: completed (green), failed (red), in-progress (blue), skipped (grey)
- [ ] Clicking an agent block shows its input, output, and reasoning
- [ ] Timeline renders within 1 second of opening the email detail

---

### US-006 — Agent Conflict Resolution

**As an** Operations Analyst,
**I want** the system to detect and resolve disagreements between agents,
**So that** I can see when the AI had conflicting signals and how it resolved them.

**Business Value:** Demonstrates sophisticated multi-agent reasoning — a key hackathon differentiator.
**Priority:** Must Have (for demo)
**Dependencies:** FR-006

**Acceptance Criteria:**
- [ ] Conflict indicator displayed on email card when conflict was detected
- [ ] Conflict detail shows: which agents disagreed, what each said, resolution logic
- [ ] Resolution displayed in plain language ("Document analysis overrode email subject classification because...")
- [ ] Conflict events logged in audit trail

---

## Epic: Classification

### US-007 — Email Type Classification

**As an** Operations Analyst,
**I want** each email to be automatically classified by type,
**So that** it is routed to the right team without me reading it first.

**Business Value:** Directly addresses the primary pain point of manual triage.
**Priority:** Must Have
**Dependencies:** FR-007

**Acceptance Criteria:**
- [ ] Classification result shown with type label and confidence percentage
- [ ] Classification reasoning shown as 1–3 plain-English sentences
- [ ] Classification available within 10 seconds of ingestion
- [ ] Correct for standard email types in ≥ 85% of test cases

---

### US-008 — Classification Confidence Indicators

**As an** Operations Analyst,
**I want to** see a visual confidence indicator for each classification,
**So that** I know at a glance which classifications need my review.

**Business Value:** Enables fast triage of the human review queue by prioritizing low-confidence items.
**Priority:** Must Have
**Dependencies:** FR-007, FR-022

**Acceptance Criteria:**
- [ ] Confidence shown as percentage and color band (green ≥ 85, amber 65–84, red < 65)
- [ ] Low-confidence emails visually distinct in the email list
- [ ] Confidence label: "High Confidence", "Review Recommended", "Manual Review Required"

---

### US-009 — Manual Classification Override

**As an** Operations Analyst,
**I want to** override an incorrect automatic classification,
**So that** I can correct errors and contribute to system learning.

**Business Value:** Maintains human control and feeds the learning loop.
**Priority:** Should Have
**Dependencies:** US-007, FR-020

**Acceptance Criteria:**
- [ ] "Override Classification" action available on every email
- [ ] Override UI shows a dropdown of all available categories
- [ ] Override saved with reviewer identity and timestamp
- [ ] Override triggers reclassification of downstream data (re-routes if needed)

---

## Epic: Document Intelligence

### US-010 — PDF Invoice Detection

**As a** Finance Analyst,
**I want** the system to automatically detect PDF invoices in email attachments,
**So that** invoice data extraction begins without my manual intervention.

**Business Value:** Removes the "open and identify" step from the finance analyst's daily routine.
**Priority:** Must Have
**Dependencies:** FR-009

**Acceptance Criteria:**
- [ ] PDF attachments with invoice content detected with ≥ 90% accuracy on test set
- [ ] Detection triggers Invoice Agent automatically
- [ ] Non-invoice PDFs correctly not routed to Invoice Agent
- [ ] Detection result visible in document analysis panel

---

### US-011 — Contract Document Detection

**As a** Contract Administrator,
**I want** the system to detect contract documents in any supported format,
**So that** contracts are registered and analyzed without manual identification.

**Business Value:** Closes the "missed contract" risk that arises from inbox overload.
**Priority:** Must Have
**Dependencies:** FR-009

**Acceptance Criteria:**
- [ ] Contract detection works for PDF and DOCX formats
- [ ] Standard contract structures (MSA, NDA, SOW, SLA) correctly identified
- [ ] Detection confidence shown in document analysis panel
- [ ] Non-contract documents correctly not routed to Contract Agent

---

### US-012 — OCR for Scanned Documents

**As a** Finance Analyst,
**I want** the system to extract text from scanned image invoices (JPG/PNG),
**So that** I don't have to manually type data from scanned documents.

**Business Value:** A significant portion of vendor invoices arrive as low-quality scans in practice.
**Priority:** Should Have
**Dependencies:** FR-009, FR-011

**Acceptance Criteria:**
- [ ] OCR applied to JPG and PNG attachments
- [ ] Extracted text passed to Invoice/Contract Agent for processing
- [ ] OCR confidence score included in agent output
- [ ] Low OCR confidence (< 0.70) triggers human review flag

---

## Epic: Invoice Processing

### US-013 — Invoice Field Extraction

**As a** Finance Analyst,
**I want** all standard invoice fields extracted automatically,
**So that** I receive clean, structured data instead of raw PDFs.

**Business Value:** Eliminates the data re-entry step that is the most time-consuming part of AP processing.
**Priority:** Must Have
**Dependencies:** FR-011

**Acceptance Criteria:**
- [ ] Vendor name, invoice number, date, due date, total, currency always attempted
- [ ] Line items extracted when present
- [ ] PO reference extracted when present
- [ ] All fields show per-field confidence scores
- [ ] Extraction accuracy ≥ 95% on clean PDF invoices

---

### US-014 — Invoice Validation Report

**As a** Finance Analyst,
**I want** the system to validate invoice math and structural completeness automatically,
**So that** I only review invoices that have actual discrepancies.

**Business Value:** Catching errors before ERP entry prevents costly corrections downstream.
**Priority:** Must Have
**Dependencies:** FR-012

**Acceptance Criteria:**
- [ ] Math validation: line items vs. subtotal vs. total
- [ ] Date validation: due date after invoice date
- [ ] Completeness validation: required fields present
- [ ] Validation summary shows PASS/FAIL with specific failure reasons
- [ ] Validation failures escalate to human review

---

### US-015 — Invoice Summary for Finance Analyst

**As a** Finance Analyst,
**I want** a structured invoice summary card for every processed invoice,
**So that** I can review key information at a glance without opening the PDF.

**Business Value:** Replaces the "open PDF, find amount, open ERP, enter data" workflow with a single view.
**Priority:** Must Have
**Dependencies:** US-013, US-014

**Acceptance Criteria:**
- [ ] Summary shows: vendor, invoice #, date, due date, amount, currency, PO reference
- [ ] Validation status shown prominently
- [ ] Original PDF accessible via link/preview
- [ ] "Export to ERP" action button (outputs structured JSON for Phase 2 integration)

---

## Epic: Contract Processing

### US-016 — Contract Metadata Extraction

**As a** Contract Administrator,
**I want** party names, dates, agreement type, and governing law extracted automatically,
**So that** contract registration requires data verification rather than data entry.

**Business Value:** Reduces contract registration time from 30 minutes to under 5 minutes per contract.
**Priority:** Must Have
**Dependencies:** FR-013

**Acceptance Criteria:**
- [ ] Parties, agreement type, effective date, expiry date extracted
- [ ] Governing law and jurisdiction extracted when present
- [ ] Auto-renewal details extracted (yes/no + notice period)
- [ ] All fields show per-field confidence scores

---

### US-017 — Contract Risk Flag Dashboard

**As a** Contract Administrator,
**I want to** see risk flags identified in contracts before I read them,
**So that** I can prioritize my review time on the clauses that matter most.

**Business Value:** Focuses human expertise on genuine risk rather than routine verification.
**Priority:** Must Have
**Dependencies:** FR-014

**Acceptance Criteria:**
- [ ] Risk flags shown in priority order (HIGH first)
- [ ] Each flag shows: flag type, severity, relevant excerpt, page reference
- [ ] Overall risk summary (e.g., "2 HIGH, 1 MEDIUM risk flags detected")
- [ ] No-risk contracts clearly marked "No Risk Flags Detected"

---

### US-018 — Contract Renewal Tracking

**As a** Contract Administrator,
**I want** the system to extract and record contract renewal and expiry dates,
**So that** I never miss an auto-renewal window.

**Business Value:** Missed auto-renewal windows are a common and costly business problem.
**Priority:** Should Have
**Dependencies:** US-016

**Acceptance Criteria:**
- [ ] Expiry date stored in contract record
- [ ] Auto-renewal flag and notice period stored
- [ ] Calculated alert date = expiry_date - notice_period - buffer stored
- [ ] Alert record created for future notification (Phase 2 delivery mechanism)

---

## Epic: Taxonomy Evolution

### US-019 — Unknown Email Detection

**As an** Operations Analyst,
**I want** the system to recognize when an email type is new and unknown,
**So that** novel communication types don't get silently misclassified.

**Business Value:** Ensures system accuracy doesn't degrade silently as business communications evolve.
**Priority:** Must Have
**Dependencies:** FR-015

**Acceptance Criteria:**
- [ ] UNKNOWN category returned when confidence across all types < 0.50
- [ ] Unknown emails flagged visually in the inbox dashboard
- [ ] Unknown emails do not trigger automated business actions
- [ ] All unknown emails routed to human review

---

### US-020 — New Category Proposal Review

**As an** Operations Analyst,
**I want to** review AI-generated proposals for new email categories,
**So that** I can approve, modify, or reject the system's learning suggestions.

**Business Value:** Keeps human expertise in control of the system's knowledge expansion.
**Priority:** Must Have
**Dependencies:** FR-016

**Acceptance Criteria:**
- [ ] Proposal shows: suggested name, evidence signals, 3 sample emails, suggested routing
- [ ] Reviewer can rename the category
- [ ] Reviewer can adjust routing suggestion
- [ ] Reviewer can see all 3 sample emails before approving
- [ ] Actions: [Approve] [Modify & Approve] [Dismiss]

---

### US-021 — Taxonomy Category Browser

**As an** Operations Analyst,
**I want to** browse and manage the active taxonomy categories,
**So that** I maintain oversight of what the system knows and how it classifies things.

**Business Value:** Gives operations visibility and governance over the evolving classification model.
**Priority:** Could Have
**Dependencies:** US-020

**Acceptance Criteria:**
- [ ] List of all active categories with: name, created date, sample count, routing
- [ ] Each category shows its key classification signals
- [ ] Category editing: rename, change routing, add signals
- [ ] Category deactivation (soft delete)
- [ ] Taxonomy version history accessible

---

### US-022 — Automatic Retroactive Reclassification

**As an** Operations Analyst,
**I want** previously unknown emails to be reclassified when a new matching category is created,
**So that** the inbox history reflects our current understanding.

**Business Value:** Prevents historical records from being permanently labelled as "unknown" after the knowledge gap is filled.
**Priority:** Should Have
**Dependencies:** US-020, FR-017

**Acceptance Criteria:**
- [ ] Upon category creation, the 3 founding sample emails are reclassified
- [ ] Reclassification logged with reason: "Taxonomy category created — retroactive classification"
- [ ] Reclassified emails visible in the new category's email list
- [ ] Reclassification does not overwrite human corrections

---

## Epic: Human Collaboration

### US-023 — Human Review Queue Access

**As an** Operations Analyst,
**I want** a dedicated review queue for emails that need my attention,
**So that** I never miss a human-review request buried in email notifications.

**Business Value:** Centralizes oversight; prevents review tasks from being lost.
**Priority:** Must Have
**Dependencies:** FR-018

**Acceptance Criteria:**
- [ ] Review queue accessible from main navigation
- [ ] Items sorted by priority (URGENT first), then by age
- [ ] Item count shown on queue nav badge
- [ ] Queue refreshes automatically every 30 seconds

---

### US-024 — Structured Review Interface for Invoices

**As a** Finance Analyst,
**I want** a review interface that shows the invoice image alongside extracted data,
**So that** I can verify and correct fields without switching between the PDF and a form.

**Business Value:** Reduces review friction; faster, more accurate corrections.
**Priority:** Must Have
**Dependencies:** FR-019

**Acceptance Criteria:**
- [ ] Side-by-side view: document image on left, extracted fields on right
- [ ] Low-confidence fields highlighted in red
- [ ] Fields editable inline with autocomplete where applicable
- [ ] Submit button disabled until required fields are populated

---

### US-025 — Review Decision Audit Log

**As an** Operations Analyst,
**I want** every human review decision to be logged with who reviewed it and what was changed,
**So that** we have a complete audit trail for compliance purposes.

**Business Value:** Required for financial and legal compliance; builds organizational accountability.
**Priority:** Must Have
**Dependencies:** FR-019, NFR-AUD-02

**Acceptance Criteria:**
- [ ] Log entry created on every review action
- [ ] Entry includes: reviewer identity, timestamp, action, corrections (before/after per field)
- [ ] Log accessible from email detail view under "Audit Trail"
- [ ] Log entries cannot be edited or deleted

---

## Epic: Explainability

### US-026 — Agent Reasoning Chain View

**As an** Operations Analyst,
**I want to** see a step-by-step explanation of how the AI reached its conclusion,
**So that** I can build trust in the system and catch reasoning errors.

**Business Value:** Trust is the gating factor for adoption; explainability drives trust.
**Priority:** Must Have
**Dependencies:** FR-021

**Acceptance Criteria:**
- [ ] Reasoning chain shows each agent's contribution in plain English
- [ ] Reasoning shown in chronological order of agent execution
- [ ] Each step shows: agent name, action taken, output, confidence
- [ ] Available for all processed emails, not just escalated ones

---

### US-027 — Per-Field Confidence Breakdown

**As a** Finance Analyst,
**I want to** see the confidence score for every extracted invoice field,
**So that** I know exactly which fields to verify and which I can trust.

**Business Value:** Focuses review time on genuinely uncertain fields; accelerates approval.
**Priority:** Must Have
**Dependencies:** FR-022

**Acceptance Criteria:**
- [ ] Every extracted field shows a confidence indicator
- [ ] Green (≥ 0.90): verified with high confidence
- [ ] Amber (0.70–0.89): likely correct, spot-check recommended
- [ ] Red (< 0.70): uncertain, manual verification required
- [ ] Confidence percentage visible on hover/tap

---

## Epic: Dashboard & Visualization

### US-028 — Real-Time Processing Dashboard

**As an** Operations Analyst,
**I want** a live dashboard showing current inbox processing activity,
**So that** I can monitor the system's health and catch problems early.

**Business Value:** Essential for operational oversight and hackathon demo impact.
**Priority:** Must Have
**Dependencies:** FR-023

**Acceptance Criteria:**
- [ ] Shows: total emails today, processed today, in queue, awaiting review
- [ ] Shows: breakdown by email type (chart)
- [ ] Shows: active agent executions
- [ ] Auto-refreshes without manual reload

---

### US-029 — Email Processing History View

**As an** Operations Analyst,
**I want to** browse and search the history of all processed emails,
**So that** I can find past processing results and audit trails.

**Business Value:** Supports operations, compliance reviews, and user confidence in system history.
**Priority:** Must Have
**Dependencies:** FR-024

**Acceptance Criteria:**
- [ ] Filterable by: date range, type, sender, status, confidence range
- [ ] Each row shows: subject, sender, type, confidence, status, processing time
- [ ] Clicking a row opens full detail view with reasoning chain
- [ ] Export to CSV for reporting

---

### US-030 — Agent Performance Metrics

**As an** Operations Analyst,
**I want to** see accuracy and performance metrics for each agent over time,
**So that** I can identify which agents are performing well and which need attention.

**Business Value:** Enables data-driven system improvement and identifies problem areas.
**Priority:** Could Have
**Dependencies:** US-028

**Acceptance Criteria:**
- [ ] Per-agent metrics: total invocations, average confidence, error rate, average duration
- [ ] Human correction rate per agent (% of times agent output was corrected)
- [ ] Trend chart: accuracy over last 7/30 days
- [ ] Drill-down to individual executions for each metric

---

### US-031 — Taxonomy Category Statistics

**As an** Operations Analyst,
**I want to** see volume statistics per taxonomy category,
**So that** I can understand the distribution of incoming communications.

**Business Value:** Informs staffing decisions, capacity planning, and process improvement.
**Priority:** Could Have
**Dependencies:** US-021

**Acceptance Criteria:**
- [ ] Bar or pie chart showing email volume by category for configurable date range
- [ ] Table showing: category, volume, auto-processed %, human-reviewed %, average confidence
- [ ] Comparison across time periods (e.g., this week vs. last week)

---

### US-032 — Human Review Workload View

**As an** Operations Analyst,
**I want to** see how many review tasks are assigned to each reviewer,
**So that** I can redistribute workload when someone is overloaded.

**Business Value:** Prevents review bottlenecks that delay downstream business actions.
**Priority:** Could Have
**Dependencies:** US-023

**Acceptance Criteria:**
- [ ] Shows: tasks per reviewer (open, completed today, average resolution time)
- [ ] Tasks overdue > 2 hours highlighted
- [ ] Admin can reassign tasks between reviewers
- [ ] Summary visible in Operations Analyst dashboard view

---

## User Story Summary

| Story ID | Epic | Priority |
|----------|------|----------|
| US-001 | Email Ingestion | Must Have |
| US-002 | Email Ingestion | Should Have |
| US-003 | Email Ingestion | Must Have |
| US-004 | Agent Orchestration | Must Have |
| US-005 | Agent Orchestration | Must Have |
| US-006 | Agent Orchestration | Must Have |
| US-007 | Classification | Must Have |
| US-008 | Classification | Must Have |
| US-009 | Classification | Should Have |
| US-010 | Document Intelligence | Must Have |
| US-011 | Document Intelligence | Must Have |
| US-012 | Document Intelligence | Should Have |
| US-013 | Invoice Processing | Must Have |
| US-014 | Invoice Processing | Must Have |
| US-015 | Invoice Processing | Must Have |
| US-016 | Contract Processing | Must Have |
| US-017 | Contract Processing | Must Have |
| US-018 | Contract Processing | Should Have |
| US-019 | Taxonomy Evolution | Must Have |
| US-020 | Taxonomy Evolution | Must Have |
| US-021 | Taxonomy Evolution | Could Have |
| US-022 | Taxonomy Evolution | Should Have |
| US-023 | Human Collaboration | Must Have |
| US-024 | Human Collaboration | Must Have |
| US-025 | Human Collaboration | Must Have |
| US-026 | Explainability | Must Have |
| US-027 | Explainability | Must Have |
| US-028 | Dashboard | Must Have |
| US-029 | Dashboard | Must Have |
| US-030 | Dashboard | Could Have |
| US-031 | Dashboard | Could Have |
| US-032 | Dashboard | Could Have |
