# Section 05 — Non-Functional Requirements

---

## Security

### NFR-SEC-01 — Authentication and Authorization

The system shall require authenticated access to all UI and API endpoints. For MVP, authentication via OAuth 2.0 / Microsoft Entra ID is the target. No unauthenticated access to email content, agent outputs, or human review queues.

**Targets:**
- All API endpoints protected by Bearer token authentication
- Role-based access: REVIEWER, ADMIN, READ_ONLY
- Admin-only access to taxonomy management and system configuration

### NFR-SEC-02 — Data in Transit Encryption

All communication between the client, API layer, and agent infrastructure shall use TLS 1.2 or higher.

**Targets:**
- HTTPS enforced on all endpoints
- No HTTP fallback permitted
- Certificate validation enforced

### NFR-SEC-03 — Data at Rest Protection

Email content, attachment data, and extracted business information shall be encrypted at rest.

**Targets:**
- AES-256 encryption for stored email content and attachments
- Database encryption enabled
- Encryption keys managed via Azure Key Vault (target infrastructure)

### NFR-SEC-04 — Attachment Sandboxing

Attachment processing (OCR, content extraction) shall occur in an isolated execution environment to prevent malicious document exploitation.

**Targets:**
- Attachment processing in isolated container with no network access
- File type validation before processing (MIME type + magic bytes)
- File size limit: 25 MB per attachment, 100 MB per email total

### NFR-SEC-05 — Sensitive Data Handling

The system shall not log full email body content or attachment binary data in application logs. Log entries reference email IDs only.

**Targets:**
- PII redaction in logs (email addresses obfuscated)
- Agent reasoning logs reference field names, not raw values for sensitive fields
- Audit trail stored separately from application logs

---

## Performance

### NFR-PERF-01 — End-to-End Processing Time

From email ingestion to final outcome (automated or human review queue), the total elapsed time shall not exceed 30 seconds for standard email + single PDF attachment under normal load.

**Targets:**
- Simple classification (no attachments): < 5 seconds
- Email + single PDF invoice: < 15 seconds
- Email + complex contract DOCX: < 25 seconds
- Email requiring human escalation: < 5 seconds to queue entry

### NFR-PERF-02 — API Response Times

All synchronous API endpoints shall return responses within defined latency budgets.

**Targets:**
- Email ingest endpoint: < 2 seconds acknowledgment
- Status check endpoint: < 500ms
- Dashboard data endpoint: < 1 second
- Human review queue endpoint: < 1 second

### NFR-PERF-03 — Agent Execution Timeout

Individual agent executions shall not block the pipeline beyond maximum duration limits.

**Targets:**
- Classification Agent: max 10 seconds
- Document Understanding Agent: max 30 seconds
- Invoice Agent: max 20 seconds
- Contract Agent: max 30 seconds
- Taxonomy Evolution Agent: max 15 seconds
- Human Collaboration Agent: immediate (async notification only)

---

## Scalability

### NFR-SCAL-01 — Concurrent Email Processing

The system shall process multiple emails concurrently without degradation in per-email performance.

**Targets:**
- MVP: 10 concurrent email processing jobs
- Phase 2: 100 concurrent jobs
- Agent pool scales horizontally

### NFR-SCAL-02 — Taxonomy Size

The taxonomy shall support up to 500 active categories without performance degradation in classification.

**Targets:**
- MVP: up to 20 categories (demo scenario)
- Phase 2: 100+ categories
- Classification performance must not degrade linearly with taxonomy size

### NFR-SCAL-03 — Storage Growth

The system shall manage email and agent execution data growth without requiring manual intervention.

**Targets:**
- Configurable retention policy per data type
- Archiving strategy for emails older than retention window
- Storage growth projections documented for Phase 2 planning

---

## Explainability

### NFR-EXPL-01 — Human-Readable Agent Reasoning

Every agent decision must produce a reasoning text that a non-technical business user can read and understand.

**Targets:**
- Reasoning texts are plain English sentences, no JSON or technical jargon
- Maximum reading time for any single agent reasoning: 30 seconds
- Business terms used consistently with organization's vocabulary

### NFR-EXPL-02 — Decision Traceability

Any final outcome of the system must be fully traceable back to: the input email, every agent that processed it, every confidence score assigned, and any human interactions.

**Targets:**
- Complete traceability chain accessible in UI within 2 clicks from any email record
- Traceability chain exportable as PDF or JSON
- No outcome is produced without a traceable chain of evidence

### NFR-EXPL-03 — Confidence Score Accuracy

Confidence scores reported by agents must be calibrated — a score of 0.90 should mean the agent is correct approximately 90% of the time in that confidence band.

**Targets:**
- Calibration measured over time using human correction feedback
- Calibration reports available to system administrators
- Systematic overconfidence (reported 0.90 but correct < 70%) triggers system alert

---

## Observability

### NFR-OBS-01 — Structured Logging

All system components shall produce structured logs in JSON format with consistent field naming.

**Targets:**
- Log fields: timestamp, level, component, email_id, agent_id, event_type, message, metadata
- Logs emitted to centralized log aggregation (Azure Monitor / Application Insights for MVP)
- Log retention: 30 days minimum

### NFR-OBS-02 — Distributed Tracing

All requests shall carry a correlation ID that links all log entries and agent executions across a single email processing job.

**Targets:**
- `correlation_id` present in all log entries for a given email processing job
- Correlation ID queryable to reconstruct full processing timeline
- Trace spans created per agent execution

### NFR-OBS-03 — Metrics and Alerting

Key operational metrics shall be collected and exposed for monitoring.

**Targets:**
- Metrics: email_processing_rate, agent_error_rate, human_review_rate, average_processing_time, queue_depth
- Alert on: agent error rate > 5%, queue depth > 50, processing time P95 > 60 seconds
- Dashboard metrics refreshed every 10 seconds

---

## Reliability

### NFR-REL-01 — Agent Failure Isolation

A failure in one agent shall not block the overall processing pipeline for other agents or emails.

**Targets:**
- Agent execution failures are caught, logged, and escalated to human review
- Processing pipeline continues for other concurrent emails
- No single agent failure cascades to system-wide failure

### NFR-REL-02 — Idempotent Email Processing

Submitting the same email multiple times shall not create duplicate records or trigger duplicate agent executions.

**Targets:**
- Emails deduplicated by hash of sender + subject + timestamp + body
- Duplicate submission returns existing email_id with current status
- Idempotency key supported on ingest API

### NFR-REL-03 — Graceful Degradation

When dependent services (LLM API, document extraction service) are unavailable, the system shall degrade gracefully and route affected emails to human review.

**Targets:**
- LLM API timeout or error: email routed to human review with error explanation
- Document extraction failure: email processed without document analysis, flagged for manual attachment review
- All degradation events logged and alerted

---

## Auditability

### NFR-AUD-01 — Immutable Audit Trail

All system decisions — including agent outputs, human approvals, corrections, and taxonomy changes — shall be recorded in an append-only audit log.

**Targets:**
- Audit records cannot be modified or deleted (within retention period)
- Each record includes: who (agent or human identity), what (action), when (timestamp), why (reasoning or correction note)
- Audit log accessible to administrators

### NFR-AUD-02 — Human Decision Logging

All human review decisions shall be logged with the reviewer's identity, the decision made, any corrections applied, and the elapsed review time.

**Targets:**
- Reviewer identity linked to authentication system identity
- Decision timestamp recorded
- Correction delta (before vs. after) stored when fields are corrected
- Review time (duration from task open to decision submit) recorded

### NFR-AUD-03 — Taxonomy Change Log

All changes to the taxonomy (new categories, modifications, deletions) shall be recorded with full context.

**Targets:**
- Taxonomy changes are versioned (v1, v2, v3...)
- Change record includes: changed_by, changed_at, change_type, previous_value, new_value, trigger (human or agent proposal)
- Taxonomy history queryable by administrators
