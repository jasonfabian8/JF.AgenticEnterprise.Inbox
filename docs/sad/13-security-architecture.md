# Section 13 — Security Architecture

---

## Security Posture

The MVP targets a **controlled demonstration environment** with security foundations in place but full enforcement deferred to Phase 2. Every security concern is documented with its MVP treatment and its Phase 2 implementation target. No security concern is ignored — the difference is enforcement level, not awareness.

---

## Authentication

### MVP

Authentication middleware is registered but configured in **passthrough mode**. All requests are processed as an anonymous system user (`system@aei.demo`). This eliminates Azure Entra ID setup requirements during the hackathon while keeping the middleware slot open.

```
Program.cs:
  builder.Services.AddAuthentication()        // registered
  ...
  app.UseAuthentication();                    // in pipeline
  app.UseAuthorization();                     // in pipeline
  // All endpoints: no [Authorize] attribute in MVP
```

### Phase 2

**Microsoft Entra ID** (Azure AD) via OpenID Connect. The application registers as an App Registration in the tenant. Users authenticate via the OAuth 2.0 Authorization Code Flow with PKCE. The React frontend uses `@azure/msal-react` for browser-side authentication. Bearer tokens are validated in the ASP.NET Core middleware.

```
Token flow:
  Browser → Entra ID /authorize → auth code
  Browser → Entra ID /token → access token (JWT)
  Browser → API: Authorization: Bearer {token}
  API: validates token signature + claims + audience
```

---

## Authorization

### MVP

No role-based access control (RBAC) enforced. All authenticated (and in MVP, anonymous) users can access all endpoints.

### Phase 2 — Role Model

| Role | Capabilities |
|------|-------------|
| `VIEWER` | Read email list, email detail, taxonomy browser, dashboard |
| `REVIEWER` | All VIEWER + submit review decisions, approve taxonomy proposals |
| `ADMIN` | All REVIEWER + create/modify taxonomy categories, system configuration |

Roles are defined as application roles in the Entra ID App Registration and mapped to claims in the JWT. Authorization policies enforce role requirements per endpoint group.

---

## Secret Management

### MVP

Secrets (Azure OpenAI API key, connection strings) are stored in `appsettings.Development.json` (excluded from Git via `.gitignore`) or environment variables. **No secrets are committed to the repository.**

Required environment variables / user secrets:
```
AzureOpenAI__ApiKey
AzureOpenAI__Endpoint
AzureOpenAI__DeploymentName
ConnectionStrings__DefaultConnection
```

### Phase 2

**Azure Key Vault** via the `Azure.Extensions.AspNetCore.Configuration.Secrets` NuGet package. The Key Vault URI is the only secret in `appsettings.json`. All other secrets are read from Key Vault at startup using Managed Identity authentication — no credentials stored anywhere in the application or configuration.

```
Startup:
  builder.Configuration.AddAzureKeyVault(
      new Uri(keyVaultUri),
      new ManagedIdentityCredential()  ← no secrets needed
  );
```

---

## AI Prompt Security

### Prompt Injection

The primary AI-specific security risk is **prompt injection** — where malicious content in an email body attempts to override agent instructions.

**Mitigations:**

1. **System prompt isolation**: Agent system prompts are set in the `SystemMessage` role of the chat history. User-controlled content (email body, attachment text) is inserted in the `UserMessage` role. The LLM treats these with different authority levels.

2. **Content truncation**: Email body text is truncated to a configurable token budget before injection into prompts. This limits the attack surface for extremely long injection attempts.

3. **Structured output enforcement**: Agents use Semantic Kernel's structured output mode. The LLM is constrained to produce valid JSON matching a schema. Injected instructions that try to produce arbitrary text responses will fail schema validation and be rejected.

4. **Output validation**: All LLM outputs pass through a validation layer before being persisted or acted upon. Unexpected field types, out-of-range confidence scores, or malformed reasoning text trigger an agent failure and human escalation — not silent acceptance of potentially injected content.

5. **No tool invocation in MVP**: Agents do not execute external tools or code in response to email content. The attack surface for tool-based prompt injection is zero in the MVP architecture.

### Data Leakage via Prompts

LLM prompts contain email content. Precautions:
- Azure OpenAI processes data within the Azure tenant — email content does not leave the Azure boundary
- `AbusePrevention` logging is disabled via Azure OpenAI configuration to prevent email content from appearing in Microsoft's abuse monitoring logs (enterprise tier feature)
- Prompt content is logged at DEBUG level only — not in production log sinks

---

## Data Protection

### Data in Transit

- All API and SignalR traffic is served over HTTPS (TLS 1.2+)
- Azure App Service enforces HTTPS-only with HTTP-to-HTTPS redirect
- SignalR WebSocket connections use WSS (WebSocket Secure)

### Data at Rest

- **MVP (SQLite)**: Database file stored on the App Service local disk. Azure App Service provides OS-level encryption for local storage.
- **Phase 2 (Azure SQL / PostgreSQL)**: Transparent Data Encryption (TDE) enabled by default on Azure managed database services.
- **Attachments**: Azure Blob Storage with Storage Service Encryption (SSE) using Microsoft-managed keys (Phase 2); local filesystem on App Service disk (MVP).

### Sensitive Field Handling

Email body content and attachment text are classified as sensitive. The following controls apply:

- Not logged in application logs at any level (only `emailId` is logged)
- Not included in OpenTelemetry spans
- Not returned in API error responses (stack traces excluded)
- Stored with reference by path in blob storage for large attachments (Phase 2)

---

## Audit Controls

All security-relevant events are recorded in `AuditEntries`:

| Event | Actor | Recorded |
|-------|-------|----------|
| Email ingested | SYSTEM | emailId, source, receivedAt |
| Agent decision made | AGENT | agentType, confidence, reasoning |
| Classification overridden | HUMAN | original, corrected, reviewer identity |
| Review decision submitted | HUMAN | action, corrections, reviewer identity, timestamp |
| Taxonomy proposal approved/dismissed | HUMAN | proposal, decision, reviewer identity |
| Category created or modified | HUMAN / SYSTEM | before/after state |

The `AuditEntries` table is append-only. In Phase 2, audit entries are also exported to Azure Monitor Log Analytics for SIEM integration and long-term retention.

---

## CORS Configuration

The API configures CORS to allow the React frontend origin:

```
Development: http://localhost:5173 (Vite dev server)
Production:  https://aei-app.azurewebsites.net (same origin via reverse proxy)
```

In production, the React SPA is served as static files from the .NET application itself (via `UseStaticFiles`), making the frontend and API the same origin — CORS is not required.

---

## Security Checklist (MVP Minimum Bar)

| Control | MVP Status | Phase 2 |
|---------|-----------|---------|
| HTTPS enforced | Yes (App Service) | Yes |
| No secrets in code | Yes (.gitignore + env vars) | Yes (Key Vault) |
| Prompt injection mitigations | Yes (role separation + schema enforcement) | Yes + red-team testing |
| Audit trail | Yes (AuditEntries table) | Yes + SIEM export |
| Data encryption in transit | Yes (TLS) | Yes |
| Data encryption at rest | Partial (OS-level disk) | Full (TDE + SSE) |
| Authentication | None enforced | Entra ID |
| Authorization (RBAC) | None enforced | Role-based per endpoint |
| PII in logs | Prevented | Prevented + PII scanning |
| Input validation | Yes (request validators) | Yes + WAF |
