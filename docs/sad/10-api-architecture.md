# Section 10 — API Architecture

---

## REST API Standards

### Base URL

```
Development:  http://localhost:5000/api/v1
Production:   https://aei-app.azurewebsites.net/api/v1
```

### Conventions

| Convention | Value |
|------------|-------|
| Versioning | URL path: `/api/v1/` |
| Authentication | Bearer token (MVP: none; Phase 2: Entra ID) |
| Content-Type | `application/json` |
| Date format | ISO 8601 UTC: `2024-11-15T10:30:00Z` |
| IDs | ULID strings: `01JF8X9K2M3N4P5Q6R7S8T9U` |
| Confidence | `float [0.0 – 1.0]` |
| Pagination | `?page=1&pageSize=20` (1-based, max 100) |
| Sorting | `?sortBy=receivedAt&sortDir=desc` |
| Filtering | `?status=COMPLETED_AUTO&type=INVOICE` |
| Error format | RFC 7807 Problem Details |
| Success 200/201 | Resource returned in body |
| Async accept | 202 Accepted with tracking ID |
| Delete | 204 No Content |

---

## Versioning Strategy

The API uses **URL path versioning** (`/api/v1/`). This approach was chosen over header versioning because:
- Visible in browser address bar and logs (easier to debug)
- Simpler to configure in ASP.NET Core Minimal APIs
- No additional client HTTP header configuration required

Version increments on **breaking changes only**. New optional fields in responses and new optional parameters in requests are backward-compatible and do not require a version bump. Version sunset policy: prior version supported for 6 months after successor release (Phase 2+).

---

## Error Handling

All errors return RFC 7807 Problem Details:

```json
{
  "type": "https://aei.api/errors/validation-error",
  "title": "Validation Error",
  "status": 422,
  "detail": "The email content field is required",
  "instance": "/api/v1/emails/ingest",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": {
    "rawContent": ["Field is required and must be non-empty"]
  }
}
```

**Error Mapping Table:**

| HTTP Status | When Used |
|-------------|-----------|
| 400 Bad Request | Malformed request body (JSON parse failure) |
| 404 Not Found | Resource ID does not exist |
| 409 Conflict | Idempotency key already exists |
| 422 Unprocessable Entity | Business validation failure |
| 500 Internal Server Error | Unhandled exception (details hidden in production) |
| 503 Service Unavailable | LLM API unavailable; graceful degradation active |

The `ErrorHandlingMiddleware` catches all unhandled exceptions and maps them to appropriate Problem Details responses. Agent failures do not return 500 — they result in a workflow state change to `AWAITING_REVIEW` or `FAILED`, reported via the status API.

---

## Endpoint Catalog

### Email Endpoints — `/api/v1/emails`

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| POST | `/emails/ingest` | Ingest a new email | 202 `{email_id, status}` |
| GET | `/emails` | List emails with filters | 200 `PagedResult<EmailSummary>` |
| GET | `/emails/{id}` | Get full email detail | 200 `EmailDetail` |
| GET | `/emails/{id}/audit` | Get full audit trail | 200 `AuditEntry[]` |
| GET | `/emails/{id}/workflow` | Get workflow and agent executions | 200 `WorkflowDetail` |

---

### Review Endpoints — `/api/v1/reviews`

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| GET | `/reviews/queue` | Get human review queue | 200 `PagedResult<ReviewTask>` |
| GET | `/reviews/{id}` | Get review task detail | 200 `ReviewDetail` |
| POST | `/reviews/{id}/decision` | Submit human decision | 200 `ReviewDecisionResult` |

---

### Taxonomy Endpoints — `/api/v1/taxonomy`

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| GET | `/taxonomy/categories` | List active categories | 200 `TaxonomyCategory[]` |
| POST | `/taxonomy/categories` | Create category manually | 201 `TaxonomyCategory` |
| PUT | `/taxonomy/categories/{id}` | Update category | 200 `TaxonomyCategory` |
| GET | `/taxonomy/proposals` | List pending proposals | 200 `TaxonomyProposal[]` |
| POST | `/taxonomy/proposals/{id}/approve` | Approve proposal | 201 `TaxonomyCategory` |
| POST | `/taxonomy/proposals/{id}/dismiss` | Dismiss proposal | 204 |

---

### Dashboard Endpoints — `/api/v1/dashboard`

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| GET | `/dashboard/summary` | Real-time summary metrics | 200 `DashboardSummary` |

---

### Health Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Overall health (liveness) |
| GET | `/health/ready` | Readiness (DB + LLM API reachable) |

---

## Response Contracts

### EmailSummary

```typescript
interface EmailSummary {
  emailId: string;
  subject: string;
  senderEmail: string;
  senderName: string;
  receivedAt: string;           // ISO 8601
  status: EmailStatus;
  classificationType: string;
  classificationConfidence: number;
  processingDurationMs: number;
  hasConflict: boolean;
  humanReviewed: boolean;
  attachmentCount: number;
}
```

### EmailDetail

```typescript
interface EmailDetail {
  emailId: string;
  subject: string;
  senderEmail: string;
  senderName: string;
  bodyPlainText: string;
  receivedAt: string;
  ingestedAt: string;
  processedAt: string | null;
  status: EmailStatus;
  processingDurationMs: number;
  attachments: AttachmentDetail[];
  classification: ClassificationDetail | null;
  invoiceExtraction: InvoiceExtractionDetail | null;
  contractExtraction: ContractExtractionDetail | null;
  agentExecutions: AgentExecutionDetail[];
  conflict: ConflictReport | null;
  humanReview: HumanReviewSummary | null;
}
```

### AgentExecutionDetail

```typescript
interface AgentExecutionDetail {
  executionId: string;
  agentType: AgentType;
  status: 'COMPLETED' | 'FAILED' | 'TIMEOUT';
  confidenceScore: number;
  reasoningText: string;
  flags: string[];
  durationMs: number;
  startedAt: string;
  completedAt: string;
  errorMessage: string | null;
}
```

### DashboardSummary

```typescript
interface DashboardSummary {
  generatedAt: string;
  today: {
    totalReceived: number;
    totalProcessed: number;
    inQueue: number;
    processing: number;
    awaitingReview: number;
    completedAuto: number;
    completedHuman: number;
    failed: number;
    autoProcessRate: number;
    avgProcessingTimeMs: number;
  };
  categoryDistribution: { type: string; count: number }[];
  activeAgents: {
    agentType: string;
    emailId: string;
    status: string;
    elapsedMs: number;
  }[];
  pendingProposals: number;
  reviewQueueDepth: number;
}
```

---

## OpenAPI / Swagger Configuration

The API generates an OpenAPI 3.0 specification using .NET's built-in `Microsoft.AspNetCore.OpenApi` package (introduced in .NET 9). The specification is served at `/openapi/v1.json` and a Swagger UI is served at `/swagger` in development.

All response types are documented using `ProducesResponseType<T>` attributes on minimal API endpoint handlers. This enables accurate OpenAPI generation without XML doc comments.

---

## Request Validation

Request payloads are validated using Fluent Validation (or built-in `IValidateOptions`). Validation failures return 422 with the `errors` field populated per field. Validation is applied in the endpoint handler before dispatching to the Application layer command handler.

Key validation rules:
- `rawContent` must be non-empty string ≤ 50,000 characters
- `attachments[].contentBase64` must be valid Base64 and ≤ 25MB decoded
- `attachments[].mimeType` must be in the allowed MIME type list
- `idempotencyKey` must be 1–200 characters if provided
- `reviews/{id}/decision.action` must be a valid `ReviewAction` enum value
- Correction `field` names must match known extraction field names
