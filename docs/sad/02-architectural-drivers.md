# Section 02 — Architectural Drivers

---

## Functional Drivers

These are the capabilities the architecture must enable. Each driver shapes specific component choices and integration patterns.

| ID | Driver | Architecture Impact |
|----|--------|---------------------|
| FD-01 | Email ingestion with attachment extraction | Document processing pipeline; blob storage; MIME parsing |
| FD-02 | Multi-agent collaborative reasoning | Agent orchestration layer; Semantic Kernel; async workflow state |
| FD-03 | Email classification with confidence scoring | LLM-backed Classification Agent; confidence threshold routing |
| FD-04 | Document type detection for attachments | Document Understanding Agent; independent classification from email |
| FD-05 | Invoice data extraction and validation | Invoice Agent; schema-constrained LLM output; math validation layer |
| FD-06 | Contract analysis and risk flag detection | Contract Agent; clause extraction; configurable risk rule engine |
| FD-07 | Taxonomy evolution with human approval | Taxonomy Evolution Agent; proposal state machine; human review queue |
| FD-08 | Human-in-the-loop review workflow | Human Collaboration Agent; review queue; structured correction capture |
| FD-09 | Real-time agent execution visualization | SignalR event streaming; React Flow graph rendering |
| FD-10 | Explainability chain for every decision | Agent execution records; reasoning text storage; audit trail |
| FD-11 | Agent conflict detection and resolution | Orchestrator conflict detection logic; resolution reasoning |
| FD-12 | Retroactive reclassification after taxonomy update | Background reclassification job; taxonomy version tracking |

---

## Non-Functional Drivers

### NFD-01 — Responsiveness

The system must provide a demonstrably live experience during the hackathon demo. Agent execution progress must appear in the UI within 1–2 seconds of each agent completing. End-to-end processing of a standard email must complete within 30 seconds.

**Architecture Impact:** SignalR for server-push events; async agent execution with progress notifications; streaming agent output where supported.

### NFD-02 — Explainability

Every classification, extraction, and routing decision must be traceable. Non-technical users must be able to read why the system made a decision. Confidence scores must be meaningful, not decorative.

**Architecture Impact:** Structured reasoning text as a first-class output of every agent; immutable AgentExecution records; per-field confidence storage; UI reasoning chain component.

### NFD-03 — Extensibility

Adding new agent types or new document categories must not require changes to the Orchestrator or the API surface. The agent workforce must be open for extension.

**Architecture Impact:** Plugin-style agent registration via Semantic Kernel; taxonomy-driven routing configuration; agent interface contracts.

### NFD-04 — Auditability

Every agent decision, human correction, and taxonomy change must be permanently recorded with actor identity and timestamp. The audit trail must be immutable within the retention window.

**Architecture Impact:** Append-only AuditEntry table; EF Core event sourcing pattern for agent executions; no soft-delete on audit entities.

### NFD-05 — Deployability

The MVP must be deployable to Azure App Service from a single repository with minimal configuration. Local development must work without Azure dependencies using SQLite and environment-specific configuration.

**Architecture Impact:** Environment-based configuration with IOptions pattern; SQLite/PostgreSQL abstraction via EF Core; Docker Compose for local orchestration.

### NFD-06 — Demo Quality

The system must perform reliably during a 5-minute live demonstration without failures. Demo scenarios must be predictable and reproducible. The UI must be visually compelling.

**Architecture Impact:** Seeded demo data; health check endpoints; graceful degradation design; UI polish as a first-class concern.

---

## Constraints

### Technical Constraints

| ID | Constraint | Rationale |
|----|------------|-----------|
| TC-01 | Backend must be C# / .NET 10 | Hackathon track and team expertise |
| TC-02 | Agent orchestration via Semantic Kernel | Microsoft ecosystem alignment; hackathon category |
| TC-03 | LLM provider: Azure OpenAI | Azure integration; enterprise-grade API; hackathon alignment |
| TC-04 | Frontend: TypeScript, React, Vite, Tailwind, shadcn/ui | Specified by project; modern toolchain |
| TC-05 | Agent workflow visualization via React Flow | Specified; best-in-class for node graph rendering in React |
| TC-06 | Real-time communication via SignalR | Specified; .NET-native WebSocket abstraction |
| TC-07 | Persistence: SQLite for MVP | Eliminates database infrastructure dependency for hackathon |
| TC-08 | Observability: OpenTelemetry + Serilog | Specified; enables Azure Monitor / Application Insights integration |
| TC-09 | Deployment target: Azure App Service | Specified; simplest Azure deployment for .NET |
| TC-10 | No microservices for MVP | Single deployable unit; reduced operational complexity |

### Organizational Constraints

| ID | Constraint | Rationale |
|----|------------|-----------|
| OC-01 | Hackathon timeline: ~4 sprints | Delivery must prioritize demonstrability over completeness |
| OC-02 | Demo duration: 5 minutes | Architecture must support a compelling, rehearsable flow |
| OC-03 | No external live mailbox integration (MVP) | Reduces setup dependencies; demo uses manual email submission |
| OC-04 | No ERP integration (MVP) | Phase 2; reduces surface area and failure points |

---

## Assumptions

| ID | Assumption | If Wrong |
|----|------------|----------|
| A-01 | Azure OpenAI API key and deployment available throughout development and demo | Fall back to OpenAI API with GPT-4o; update configuration only |
| A-02 | SQLite is sufficient for demo data volume (< 1000 emails) | Already architected for EF Core migration to PostgreSQL |
| A-03 | Single-tenant deployment is sufficient for MVP | Multi-tenant schema design deferred to Phase 2 |
| A-04 | LLM outputs are deterministic enough for confidence calibration | Implement retry-with-comparison logic for critical decisions |
| A-05 | PDF text extraction is sufficient for clean PDF invoices and contracts | OCR path via Azure Document Intelligence added for Should Have scope |
| A-06 | English-language emails only for MVP | Multilingual support is a Phase 2 item |
| A-07 | Demo hardware has a stable internet connection to Azure OpenAI | Pre-recorded fallback video prepared; demo data seeded locally |
| A-08 | Semantic Kernel .NET SDK stable enough for production-like use | Version pinned; monitor SK release notes throughout sprint cycle |
