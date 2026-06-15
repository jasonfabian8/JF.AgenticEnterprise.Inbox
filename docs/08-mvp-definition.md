# Section 08 — MVP Definition (MoSCoW Prioritization)

---

## MoSCoW Overview

| Priority | Label | Count | Rationale |
|----------|-------|-------|-----------|
| M — Must Have | Non-negotiable for MVP | 22 | Core demo scenarios + judging criteria |
| S — Should Have | High value, deliver if time allows | 5 | Improve depth and realism |
| C — Could Have | Desirable, deprioritized | 5 | Nice additions but not demo-blocking |
| W — Won't Have | Out of scope for MVP | Listed below | Defined in Non-Goals |

---

## Must Have

These capabilities are required for the hackathon demo to demonstrate all Reasoning Agents judging criteria. Without any one of these, the demo is incomplete.

### Infrastructure & Ingestion

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Email upload via web UI | US-001 | Demo entry point — judges need to submit an email |
| Processing status tracking | US-003 | Judges need to see live status progression |
| Orchestrator Agent execution | US-004 | Core Reasoning Agents criterion: multi-agent coordination |
| Agent execution timeline visualization | US-005 | Judges must see the agent collaboration happening live |
| Agent conflict resolution | US-006 | Demonstrating disagreement + resolution is a key differentiator |

### Classification

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Email type classification | US-007 | Fundamental capability; demo collapses without it |
| Classification confidence indicators | US-008 | Shows reasoning quality; judges evaluate this |

### Document Intelligence

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| PDF invoice detection | US-010 | Primary document type in demo scenario |
| Contract document detection | US-011 | Second document type in demo scenario |

### Invoice Processing

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Invoice field extraction | US-013 | Core business value demonstration |
| Invoice validation report | US-014 | Demonstrates multi-step reasoning within Invoice Agent |
| Invoice summary card | US-015 | Visual payoff for Finance Analyst persona demo |

### Contract Processing

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Contract metadata extraction | US-016 | Core business value demonstration |
| Contract risk flag dashboard | US-017 | Visually compelling; shows document intelligence depth |

### Taxonomy Evolution

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Unknown email detection | US-019 | Required for taxonomy evolution demo segment |
| New category proposal review | US-020 | The "system learns" moment — critical for demo narrative |

### Human Collaboration

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Human review queue access | US-023 | Human-in-the-loop is a judging criterion |
| Structured review interface | US-024 | Judges must see what the human reviewer sees |
| Review decision audit log | US-025 | Demonstrates auditability and accountability |

### Explainability

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Agent reasoning chain view | US-026 | Explainability is a core judging criterion |
| Per-field confidence breakdown | US-027 | Shows granularity of AI reasoning |

### Dashboard

| Item | Story | Why Non-Negotiable |
|------|-------|--------------------|
| Real-time processing dashboard | US-028 | The live demo view that judges watch during demonstration |
| Email processing history view | US-029 | Enables judges to drill into any processed email |

---

## Should Have

These items significantly improve the quality and completeness of the demo but are not blocking if time is constrained.

| Item | Story | Value Added |
|------|-------|-------------|
| Multi-attachment email processing | US-002 | More realistic demo scenarios; catches edge cases |
| Manual classification override | US-009 | Demonstrates human control over AI decisions |
| OCR for scanned documents | US-012 | Adds realism; most compelling for the human validation journey |
| Contract renewal tracking | US-018 | Completes the contract story; shows business impact beyond registration |
| Automatic retroactive reclassification | US-022 | Makes the taxonomy evolution moment more visually impactful |

---

## Could Have

These items add depth but are genuinely optional. Implement only if Sprint 5 has remaining capacity after polish and rehearsal.

| Item | Story | When Useful |
|------|-------|-------------|
| Taxonomy category browser | US-021 | If demo audience asks "how does the taxonomy work?" |
| Agent performance metrics | US-030 | If demo runs long and audience wants to explore further |
| Taxonomy category statistics | US-031 | For executive-audience demos focused on operations analytics |
| Human review workload view | US-032 | For demos targeting operations managers over analysts |
| Classification confidence threshold configuration | FR-008 | For demos targeting technical architects |

---

## Won't Have (MVP Scope Exclusions)

These are formally out of scope and should not be partially implemented.

| Item | Reason |
|------|--------|
| Live Microsoft 365 mailbox integration | Infrastructure complexity; not needed for demo |
| Microsoft Teams notifications | Phase 2; demo uses in-app queue |
| ERP system integration (Dynamics, SAP) | Phase 2; demo uses JSON export simulation |
| Contract Lifecycle Management (CLM) integration | Phase 2 |
| Multi-language support | Phase 2 |
| Outbound email drafting | Phase 2 |
| Fine-tuning or custom model training | Requires data + compute beyond hackathon scope |
| Mobile-native client | Web-first for hackathon |
| Multi-tenant architecture | Phase 3 |
| GDPR/SOC2/HIPAA compliance mode | Phase 3 |
| Processing emails with > 10 attachments | Edge case deferred |
| Proactive anomaly detection | Phase 4 |

---

## MVP Feature Map

```
AGENTIC ENTERPRISE INBOX — MVP SCOPE

┌─────────────────────────────────────────────────────┐
│                   MUST HAVE                         │
│                                                     │
│  Email Ingestion → Status Tracking                  │
│                                                     │
│  Orchestrator Agent                                 │
│  ├── Agent Timeline Visualization  ← VISIBLE        │
│  └── Conflict Detection & Resolution ← VISIBLE      │
│                                                     │
│  Classification Agent                               │
│  ├── Type + Confidence + Reasoning ← VISIBLE        │
│  └── Threshold-based Routing                        │
│                                                     │
│  Document Understanding Agent                       │
│  ├── Invoice Detection → Invoice Agent              │
│  │   ├── Field Extraction                           │
│  │   ├── Math Validation                            │
│  │   └── Summary Card ← VISIBLE                     │
│  └── Contract Detection → Contract Agent            │
│      ├── Metadata Extraction                        │
│      └── Risk Flags ← VISIBLE                       │
│                                                     │
│  Taxonomy Evolution Agent                           │
│  ├── Unknown Detection ← VISIBLE                    │
│  └── Proposal Generation ← VISIBLE                  │
│                                                     │
│  Human Collaboration Agent                          │
│  ├── Review Queue ← VISIBLE                         │
│  ├── Structured Review UI ← VISIBLE                 │
│  └── Decision Audit Log                             │
│                                                     │
│  Explainability Layer (wired into all agents)       │
│  ├── Reasoning Chain ← VISIBLE                      │
│  └── Per-field Confidence ← VISIBLE                 │
│                                                     │
│  Dashboard                                          │
│  ├── Live Activity View ← DEMO CENTERPIECE          │
│  └── Email History                                  │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  SHOULD HAVE                        │
│  Multi-attachment processing                        │
│  Classification override                            │
│  OCR for scanned images                             │
│  Contract renewal tracking                          │
│  Retroactive reclassification                       │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│                  COULD HAVE                         │
│  Taxonomy browser                                   │
│  Agent performance metrics                          │
│  Category statistics                                │
│  Workload view                                      │
│  Threshold configuration UI                         │
└─────────────────────────────────────────────────────┘
```

---

## MVP Acceptance Definition

The MVP is complete when the following demo scenarios execute end-to-end without manual intervention (except intentional human-in-the-loop steps):

1. **Happy Path Invoice:** Email with PDF invoice → classified as INVOICE (≥ 0.85 confidence) → data extracted → validation passes → summary shown → no human review required
2. **Contract with Risk Flags:** Email with DOCX contract → classified → risk flags detected → escalated to human review → reviewer approves → contract registered
3. **Unknown Email → Taxonomy Evolution:** Three unknown emails submitted → Taxonomy Evolution Agent proposes new category → human approves → category created → emails reclassified
4. **Low-Quality Invoice → Human Validation:** Scanned image invoice → OCR low confidence → escalated to human review → reviewer corrects fields → invoice processed
5. **Agent Conflict:** Email with body saying "proposal" but PDF attachment containing a signed contract → conflict detected → document evidence wins → contract workflow initiated
