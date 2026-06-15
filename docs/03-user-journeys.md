# Section 03 — User Journeys

---

## Journey 1 — Invoice Processing

### Scenario
A vendor sends an email with the subject "Invoice #INV-2024-00892 — Payment Due 30 Days" and attaches a PDF invoice.

### Trigger
Email arrives in the enterprise inbox.

---

### Step-by-Step Flow

```
EMAIL ARRIVES
     │
     ▼
[ORCHESTRATOR AGENT]
 ─ Receives email event
 ─ Extracts: sender, subject, body, attachment list
 ─ Initiates workflow: INVOICE_PROCESSING_CANDIDATE
     │
     ▼
[CLASSIFICATION AGENT]
 ─ Reads subject + body
 ─ Produces: { type: "INVOICE", confidence: 0.94, reasoning: "Subject contains invoice number pattern; body references payment terms and due date" }
 ─ Confidence ≥ 0.85 → NO human escalation at this stage
     │
     ▼
[DOCUMENT UNDERSTANDING AGENT]
 ─ Detects PDF attachment
 ─ Determines document type: INVOICE
 ─ Routes to → Invoice Agent
     │
     ▼
[INVOICE AGENT]
 ─ Extracts structured data:
   · Vendor: Acme Supplies Ltd.
   · Invoice #: INV-2024-00892
   · Date: 2024-11-15
   · Due Date: 2024-12-15
   · Amount: $14,850.00 USD
   · Tax: $2,376.00
   · Line Items: 3 items (Office Supplies, Logistics Fee, Handling)
   · PO Reference: PO-2024-4421
 ─ Validates: PO reference format valid, amounts sum correctly, due date in future
 ─ Confidence: 0.97
     │
     ▼
[ORCHESTRATOR AGENT — CONSOLIDATION]
 ─ Merges classification + extraction outputs
 ─ Cross-validates: email sender domain matches vendor name → PASS
 ─ Determines: FULLY_AUTOMATED path (all confidence ≥ threshold)
     │
     ▼
OUTCOME: Invoice record created
 · Status: PROCESSED_AUTO
 · Extracted data available for ERP export
 · Audit trail: all agent decisions logged with timestamps and reasoning
 · Finance Analyst notified: "Invoice INV-2024-00892 processed — $14,850.00 from Acme Supplies"
```

### Human Interactions
None required (fully automated path).

### Final Outcome
Structured invoice record ready for ERP import. Finance Analyst receives a summary notification. Full reasoning chain available on demand.

---

## Journey 2 — Contract Processing

### Scenario
A legal firm sends an email with a new supplier agreement attached as a Word document, with no clear subject line.

### Trigger
Email arrives with `.docx` attachment. Subject: "Re: Follow-up from Thursday's call"

---

### Step-by-Step Flow

```
EMAIL ARRIVES
     │
     ▼
[ORCHESTRATOR AGENT]
 ─ Detects ambiguous subject line
 ─ Flags for enhanced classification
     │
     ▼
[CLASSIFICATION AGENT]
 ─ Reads body: references "agreement", "terms and conditions", "effective date", "parties"
 ─ Produces: { type: "CONTRACT", confidence: 0.78, reasoning: "Body language consistent with contract context, but subject line provides no signal; attachment not yet analyzed" }
 ─ Confidence 0.78 < 0.85 → DEFERRED — wait for document analysis
     │
     ▼
[DOCUMENT UNDERSTANDING AGENT]
 ─ Opens .docx attachment
 ─ Detects: document structure has "Agreement", "WHEREAS", "IN WITNESS WHEREOF" sections
 ─ Document type: CONTRACT (confidence 0.96)
 ─ Routes to → Contract Agent
     │
     ▼
[CONTRACT AGENT]
 ─ Extracts:
   · Parties: "TechCorp Inc." (Buyer) / "Nexus Legal Partners" (Provider)
   · Agreement Type: Master Service Agreement
   · Effective Date: 2024-12-01
   · Initial Term: 24 months
   · Auto-renewal: YES — 30 days notice to cancel
   · Liability Cap: $500,000
   · Termination for Convenience: YES (90-day notice)
   · Governing Law: New York
 ─ Risk Flags:
   · [MEDIUM] Auto-renewal with short notice window (30 days)
   · [LOW] Liability cap below company standard ($1M)
 ─ Confidence: 0.89
     │
     ▼
[ORCHESTRATOR AGENT — CONSOLIDATION]
 ─ Classification Agent: 0.78 → updated with document evidence → 0.91
 ─ Combined confidence: 0.91
 ─ Risk flags detected → escalate to Human Review (Sofia, Contract Admin)
     │
     ▼
[HUMAN COLLABORATION AGENT]
 ─ Creates Human Review task:
   · Summary of extracted data
   · Risk flags highlighted
   · Confidence scores shown
   · Actions: [Approve] [Correct Data] [Flag for Legal]
     │
     ▼
SOFIA REVIEWS
 ─ Reviews auto-renewal risk flag
 ─ Approves extraction data
 ─ Adds note: "Escalate liability cap to procurement lead"
 ─ Clicks [Approve with Note]
     │
     ▼
OUTCOME:
 ─ Contract registered in CLM
 ─ Renewal date alert set: 2026-11-01 (30 days before auto-renewal)
 ─ Procurement lead notified about liability cap
 ─ Human decision logged with timestamp and note
```

### Human Interactions
Contract Administrator reviews risk-flagged contract. Approves with annotation.

### Final Outcome
Contract registered, renewal date tracked, risk flagged to appropriate stakeholder. Full audit trail including human decision reason.

---

## Journey 3 — New Category Detection

### Scenario
A wave of emails arrives from an insurance broker providing annual coverage certificates. This type has never been processed before.

### Trigger
Three emails arrive within 1 hour with attachments containing the phrase "Certificate of Insurance" — a category not in the current taxonomy.

---

### Step-by-Step Flow

```
EMAIL #1 ARRIVES
     │
     ▼
[CLASSIFICATION AGENT]
 ─ Attempts classification
 ─ Best match: "DOCUMENT_REQUEST" — confidence: 0.52
 ─ Below threshold (0.85) AND no near match in taxonomy
 ─ Flags: UNKNOWN_CATEGORY_CANDIDATE
     │
     ▼
[TAXONOMY EVOLUTION AGENT — ACTIVATED]
 ─ Examines email content and attachment
 ─ Finds: "Certificate of Insurance", "policyholder", "coverage period", "endorsement"
 ─ Searches existing taxonomy: no match
 ─ Creates candidate: { label: "Insurance Certificate", signals: [...], sample_count: 1 }
     │
     ▼
EMAIL #2 ARRIVES (same pattern)
     │
     ▼
[TAXONOMY EVOLUTION AGENT]
 ─ Matches new email to existing candidate
 ─ sample_count: 2
     │
     ▼
EMAIL #3 ARRIVES (same pattern)
     │
     ▼
[TAXONOMY EVOLUTION AGENT]
 ─ sample_count: 3 → THRESHOLD REACHED for proposal
 ─ Generates formal proposal:
   · Proposed Category: "Insurance Certificate"
   · Confidence: 0.87
   · Evidence: 3 emails, key signals listed
   · Suggested routing: Operations / Risk Management
   · Suggested extraction fields: Policyholder, Insurer, Policy Number, Coverage Period, Limits
     │
     ▼
[HUMAN COLLABORATION AGENT]
 ─ Presents proposal to Operations Analyst (Carla)
 ─ Actions: [Create Category] [Merge with Existing] [Dismiss]
     │
     ▼
CARLA REVIEWS
 ─ Reviews 3 sample emails shown alongside proposal
 ─ Renames: "Insurance Certificate" → "COI — Certificate of Insurance"
 ─ Confirms routing: Risk Management team
 ─ Clicks [Create Category]
     │
     ▼
[TAXONOMY EVOLUTION AGENT]
 ─ Adds new category to taxonomy
 ─ Retroactively reclassifies 3 emails with new category
 ─ Logs: category_created_by=human, created_at=timestamp, initial_samples=3
     │
     ▼
OUTCOME:
 ─ New taxonomy entry: COI — Certificate of Insurance
 ─ Future emails of this type classified correctly
 ─ System sends Carla confirmation: "New category active — 3 emails reclassified"
```

### Human Interactions
Operations Analyst reviews taxonomy proposal, renames, confirms routing, creates category.

### Final Outcome
New business category created. System learns from 3 examples. All future COI emails classified automatically.

---

## Journey 4 — Human Validation Workflow

### Scenario
An email arrives with an invoice that has been scanned at low resolution. The Invoice Agent extracts data but with low confidence due to OCR quality issues.

### Trigger
Email with attached scanned image (JPG) of a handwritten/low-quality invoice.

---

### Step-by-Step Flow

```
EMAIL ARRIVES
     │
     ▼
[ORCHESTRATOR AGENT + CLASSIFICATION AGENT]
 ─ Classification: INVOICE — confidence 0.91 (text in body references "please find invoice attached")
     │
     ▼
[DOCUMENT UNDERSTANDING AGENT]
 ─ Detects JPG attachment
 ─ Applies OCR
 ─ Document type: INVOICE — confidence 0.73 (image quality low)
     │
     ▼
[INVOICE AGENT]
 ─ Extracts with OCR output:
   · Vendor: "Mart?nez & Sons" (character recognition uncertain)
   · Amount: "$2,4?0.00" (digit unclear)
   · Due Date: "11/15/2?" (year incomplete)
   · PO Reference: NOT FOUND
 ─ Confidence: 0.51 — BELOW THRESHOLD
 ─ Flags: OCR_QUALITY_ISSUE, MISSING_PO_REFERENCE
     │
     ▼
[ORCHESTRATOR AGENT]
 ─ Confidence below threshold (0.51 < 0.85)
 ─ Multiple extraction failures
 ─ Escalation decision: HUMAN_REVIEW_REQUIRED
     │
     ▼
[HUMAN COLLABORATION AGENT]
 ─ Creates structured review task:
   · Shows: original image + OCR output side by side
   · Highlights: uncertain fields in red
   · Pre-fills: known good fields in green
   · Provides inline editing for each uncertain field
   · Shows: confidence scores per field
   · Actions: [Confirm & Submit] [Request Original from Sender] [Reject]
     │
     ▼
MARCUS (Finance Analyst) RECEIVES NOTIFICATION
 ─ Opens review task
 ─ Corrects: "Mart?nez & Sons" → "Martinez & Sons"
 ─ Corrects: "$2,4?0.00" → "$2,450.00"
 ─ Corrects: due date by checking calendar context
 ─ Enters: PO-2024-5519 (found by cross-referencing vendor)
 ─ Clicks [Confirm & Submit]
     │
     ▼
[HUMAN COLLABORATION AGENT → ORCHESTRATOR]
 ─ Human-corrected data submitted
 ─ Logs: corrected_by=marcus, corrections=[...], original_ocr=[...]
     │
     ▼
[TAXONOMY EVOLUTION AGENT — passive observer]
 ─ Records: low-quality image from this vendor → flag for future
 ─ No new category, but vendor OCR risk profile updated
     │
     ▼
OUTCOME:
 ─ Invoice processed with human-verified data
 ─ Audit trail: original OCR + human correction + correction delta
 ─ Finance Analyst receives confirmation
 ─ System flags: request high-quality PDF from Martinez & Sons in future
```

### Human Interactions
Finance Analyst corrects low-confidence OCR fields via structured review UI. Submits verified data.

### Final Outcome
Invoice processed with human oversight. Full correction audit trail maintained. Vendor OCR risk profile updated for future reference.

---

## Journey Summary Matrix

| Journey | Trigger | Agents Involved | Human Required | Outcome |
|---------|---------|-----------------|----------------|---------|
| Invoice Processing | PDF invoice email | Orchestrator, Classification, DocUnderstanding, Invoice | No (auto) | ERP-ready record |
| Contract Processing | Ambiguous contract email | All 7 agents | Yes (risk flags) | CLM registration + alerts |
| New Category Detection | Unknown email type (3x) | Orchestrator, Classification, TaxonomyEvolution, HumanCollab | Yes (category approval) | New taxonomy entry |
| Human Validation | Low-quality scanned invoice | Orchestrator, Classification, DocUnderstanding, Invoice, HumanCollab | Yes (OCR correction) | Human-verified record |
