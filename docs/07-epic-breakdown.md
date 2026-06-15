# Section 07 — Epic Breakdown

---

## Epic Overview

| Epic ID | Epic Name | Stories | Must Have | Should Have | Could Have |
|---------|-----------|---------|-----------|-------------|------------|
| EP-01 | Email Ingestion | US-001, US-002, US-003 | 2 | 1 | 0 |
| EP-02 | Agent Orchestration | US-004, US-005, US-006 | 3 | 0 | 0 |
| EP-03 | Classification | US-007, US-008, US-009 | 2 | 1 | 0 |
| EP-04 | Document Intelligence | US-010, US-011, US-012 | 2 | 1 | 0 |
| EP-05 | Invoice Processing | US-013, US-014, US-015 | 3 | 0 | 0 |
| EP-06 | Contract Processing | US-016, US-017, US-018 | 2 | 1 | 0 |
| EP-07 | Taxonomy Evolution | US-019, US-020, US-021, US-022 | 2 | 1 | 1 |
| EP-08 | Human Collaboration | US-023, US-024, US-025 | 3 | 0 | 0 |
| EP-09 | Explainability | US-026, US-027 | 2 | 0 | 0 |
| EP-10 | Dashboard & Visualization | US-028, US-029, US-030, US-031, US-032 | 2 | 0 | 3 |

---

## EP-01 — Email Ingestion

**Goal:** Enable emails to enter the system reliably with all content extracted and ready for agent processing.

**Why this epic comes first:** Every other epic depends on emails being correctly ingested. Without reliable ingestion, no agent can process anything.

**Risks:**
- MIME parsing complexity for edge-case email formats
- Large attachment handling affecting ingest performance
- Deduplication edge cases (forwarded emails, RE: chains)

**Definition of Done:**
- User can submit an email via UI and receive a tracking ID
- All attachments detected and stored
- Email enters the processing queue with QUEUED status

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-001 | Email Upload via Web UI | Must Have |
| US-002 | Multi-Attachment Email Processing | Should Have |
| US-003 | Processing Status Tracking | Must Have |

---

## EP-02 — Agent Orchestration

**Goal:** A single, visible Orchestrator Agent coordinates all agent activity, tracks state, resolves conflicts, and produces a unified outcome.

**Why this epic is foundational:** Without orchestration, agents operate in isolation and cannot collaborate. The Orchestrator is the "conductor" that makes the platform a coherent workforce rather than a collection of tools.

**Risks:**
- Orchestration logic becoming complex as agent count grows
- State synchronization across parallel agent executions
- Conflict resolution edge cases

**Definition of Done:**
- Every email processing job has a single Orchestrator Agent execution record
- Agent execution timeline visible in UI
- Conflict detection fires correctly when agent outputs disagree

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-004 | Orchestrated Agent Workflow | Must Have |
| US-005 | Agent Execution Timeline Visualization | Must Have |
| US-006 | Agent Conflict Resolution | Must Have |

---

## EP-03 — Classification

**Goal:** Every email receives an accurate type classification with a confidence score and human-readable reasoning within 10 seconds.

**Why this matters:** Classification drives all downstream routing. An incorrect classification early can cascade into a completely wrong workflow.

**Risks:**
- Ambiguous emails that span multiple categories
- Overconfidence in wrong classifications
- Threshold tuning requiring calibration data

**Definition of Done:**
- Classification result with confidence shown for every ingested email
- Routing paths differ correctly based on confidence thresholds
- Override mechanism available for incorrect classifications

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-007 | Email Type Classification | Must Have |
| US-008 | Classification Confidence Indicators | Must Have |
| US-009 | Manual Classification Override | Should Have |

---

## EP-04 — Document Intelligence

**Goal:** Attachments are independently analyzed, their type detected, and routing decisions made to the correct specialist agent.

**Why this is distinct from Classification:** Emails and their attachments can tell different stories. An email with a vague subject might contain a legally binding contract. Independent attachment analysis catches what email body analysis alone misses.

**Risks:**
- OCR accuracy on low-quality scans
- Document format variability (varied invoice layouts, table-less contracts)
- Processing time for large PDFs

**Definition of Done:**
- PDF and DOCX attachments correctly classified by document type
- Routing to Invoice Agent or Contract Agent triggered automatically
- OCR applied to image attachments with confidence score

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-010 | PDF Invoice Detection | Must Have |
| US-011 | Contract Document Detection | Must Have |
| US-012 | OCR for Scanned Documents | Should Have |

---

## EP-05 — Invoice Processing

**Goal:** Invoice data is fully extracted, mathematically validated, and presented as a structured, ERP-ready record.

**Why this epic has high business value:** Invoice processing is the most common and most costly manual document workflow in most organizations. Automating it delivers immediate, measurable ROI.

**Risks:**
- Invoice format diversity across vendors
- Currency and rounding rule variations
- OCR errors on scanned invoices escalating to human review

**Definition of Done:**
- All mandatory invoice fields extracted from clean PDFs with ≥ 95% accuracy
- Math validation runs automatically and produces named failure flags
- Finance Analyst can review, correct, and export a structured invoice record

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-013 | Invoice Field Extraction | Must Have |
| US-014 | Invoice Validation Report | Must Have |
| US-015 | Invoice Summary for Finance Analyst | Must Have |

---

## EP-06 — Contract Processing

**Goal:** Contract documents are identified, metadata extracted, risk flags surfaced, and renewal dates tracked without manual reading.

**Why this matters for the demo:** Contract risk flags are visually compelling — they demonstrate the system's ability to find needles in dense document haystacks.

**Risks:**
- Contract language is highly variable and jurisdiction-specific
- Risk flag false positives could undermine trust
- Distinguishing parties (buyer vs. vendor) requires context understanding

**Definition of Done:**
- Parties, dates, agreement type, and key clause indicators extracted
- Risk flags categorized by severity and shown in review UI
- Auto-renewal detection and notice period extraction working

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-016 | Contract Metadata Extraction | Must Have |
| US-017 | Contract Risk Flag Dashboard | Must Have |
| US-018 | Contract Renewal Tracking | Should Have |

---

## EP-07 — Taxonomy Evolution

**Goal:** The system learns new email categories from unknown inputs, proposes them for human approval, and retroactively applies them to historical data.

**Why this is the most differentiated capability:** Most classification systems have a fixed taxonomy. A self-evolving taxonomy demonstrates true organizational learning — the system gets smarter over time.

**Risks:**
- False positives on taxonomy proposals (grouping unrelated emails)
- Proposal threshold tuning (too sensitive vs. too slow to learn)
- User confusion about what they are approving

**Definition of Done:**
- UNKNOWN emails flagged in UI without being silently misclassified
- After 3 matching unknowns, proposal generated and presented to human
- Approved category immediately active and retroactively applied

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-019 | Unknown Email Detection | Must Have |
| US-020 | New Category Proposal Review | Must Have |
| US-021 | Taxonomy Category Browser | Could Have |
| US-022 | Automatic Retroactive Reclassification | Should Have |

---

## EP-08 — Human Collaboration

**Goal:** Humans are engaged precisely when needed, given all the information required to make a decision quickly, and their decisions are captured with full context.

**Why this is essential for trust:** The best AI system still needs a human backstop. Making that backstop efficient, organized, and auditable is what makes automation safe to adopt.

**Risks:**
- Review queue becoming backlogged if escalation rates are too high
- Reviewers making decisions without reading reasoning (leading to poor corrections)
- Review interface being too complex for non-technical reviewers

**Definition of Done:**
- Review queue accessible and filterable
- Structured review interface shows document + extracted data + reasoning side-by-side
- All decisions logged with reviewer identity, timestamp, and corrections

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-023 | Human Review Queue Access | Must Have |
| US-024 | Structured Review Interface for Invoices | Must Have |
| US-025 | Review Decision Audit Log | Must Have |

---

## EP-09 — Explainability

**Goal:** Every system decision is understandable to a business user without technical knowledge.

**Why this is a hackathon requirement:** The Reasoning Agents category specifically evaluates whether multi-step agent reasoning is transparent. This epic is directly judged.

**Risks:**
- Reasoning texts being too technical or too long
- Confidence scores being uncalibrated and misleading
- Reasoning generation adding too much latency

**Definition of Done:**
- Plain-language reasoning chain available for every processed email
- Per-field confidence indicators shown in all extraction views
- Reasoning chain shows full agent execution sequence in correct order

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-026 | Agent Reasoning Chain View | Must Have |
| US-027 | Per-Field Confidence Breakdown | Must Have |

---

## EP-10 — Dashboard & Visualization

**Goal:** A real-time dashboard gives operational visibility into inbox processing activity and provides the primary interface for the hackathon demo.

**Why the dashboard matters for the hackathon:** During a 5-minute demo, the dashboard is the "face" of the system. Judges watch live activity, agent collaboration, and human oversight happen in one view.

**Risks:**
- Real-time updates requiring WebSocket or polling architecture
- Dashboard performance under concurrent processing
- Visual design quality affecting perceived product maturity

**Definition of Done:**
- Live processing activity visible without page reload
- Email history searchable and filterable
- Per-agent performance metrics accessible

**Stories:**

| Story | Title | Priority |
|-------|-------|----------|
| US-028 | Real-Time Processing Dashboard | Must Have |
| US-029 | Email Processing History View | Must Have |
| US-030 | Agent Performance Metrics | Could Have |
| US-031 | Taxonomy Category Statistics | Could Have |
| US-032 | Human Review Workload View | Could Have |

---

## Delivery Sequence Recommendation

```
Sprint 1 (Foundation)
├── EP-01: Email Ingestion
└── EP-02: Agent Orchestration (skeleton)

Sprint 2 (Core Intelligence)
├── EP-03: Classification
├── EP-04: Document Intelligence
└── EP-09: Explainability (wired into agents)

Sprint 3 (Business Value)
├── EP-05: Invoice Processing
└── EP-06: Contract Processing

Sprint 4 (Learning & Human Loop)
├── EP-07: Taxonomy Evolution
└── EP-08: Human Collaboration

Sprint 5 (Demo Polish)
└── EP-10: Dashboard & Visualization
    + Demo rehearsal + edge case handling
```
