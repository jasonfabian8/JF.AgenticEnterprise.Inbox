# Section 05 — Logical Architecture

---

## Layer Model

The system is organized into seven logical layers following Clean Architecture principles. Dependencies are strictly unidirectional: outer layers depend on inner layers; inner layers have no knowledge of outer layers.

```mermaid
graph TB
    subgraph "Presentation Layer"
        UI_SPA["React SPA\n(TypeScript, Vite, React Flow, shadcn/ui)"]
        API_LAYER["ASP.NET Core Minimal APIs\n(REST Endpoints, SignalR Hub)"]
    end

    subgraph "Application Layer"
        CMD["Command Handlers\n(MediatR or direct dispatch)"]
        QRY["Query Handlers\n(Dashboard, Email History)"]
        WF["Workflow Coordinator\n(Orchestrates agent invocation sequence)"]
        EVT["Domain Event Dispatcher\n(Agent events → SignalR bridge)"]
    end

    subgraph "Agent Layer"
        ORCH_A["Orchestrator Agent\n(Semantic Kernel — Process)"]
        CLASS_A["Classification Agent\n(Semantic Kernel — KernelFunction)"]
        DOCUND_A["Document Understanding Agent\n(Semantic Kernel — KernelFunction)"]
        INV_A["Invoice Agent\n(Semantic Kernel — KernelFunction)"]
        CON_A["Contract Agent\n(Semantic Kernel — KernelFunction)"]
        TAX_A["Taxonomy Evolution Agent\n(Semantic Kernel — KernelFunction)"]
        HC_A["Human Collaboration Agent\n(Semantic Kernel — KernelFunction)"]
    end

    subgraph "Domain Layer"
        ENT["Domain Entities\n(Email, Workflow, AgentExecution, ...)"]
        DOM_EVT["Domain Events\n(EmailIngested, AgentCompleted, ...)"]
        IFACE["Agent Interfaces\n(IClassificationAgent, IInvoiceAgent, ...)"]
        REPO_IFACE["Repository Interfaces\n(IEmailRepository, IWorkflowRepository, ...)"]
        DOM_SVC["Domain Services\n(ConflictResolver, TaxonomyMatcher, ConfidenceEvaluator)"]
    end

    subgraph "Infrastructure Layer"
        SK_IMPL["Semantic Kernel Agent Implementations\n(Azure OpenAI integration)"]
        REPO_IMPL["EF Core Repository Implementations\n(SQLite / PostgreSQL)"]
        BLOB_IMPL["Attachment Storage Service\n(Local / Azure Blob)"]
        TEL["Telemetry\n(Serilog, OpenTelemetry, AppInsights)"]
        CFG["Configuration\n(IOptions pattern, Azure Key Vault Phase 2)"]
    end

    subgraph "Persistence Layer"
        DB[(SQLite — MVP\nPostgreSQL — Phase 2)]
        FILES[Attachment Files\nLocal / Azure Blob]
    end

    UI_SPA -->|REST + WebSocket| API_LAYER
    API_LAYER --> CMD
    API_LAYER --> QRY
    API_LAYER --> EVT
    CMD --> WF
    WF --> ORCH_A
    ORCH_A --> CLASS_A
    ORCH_A --> DOCUND_A
    DOCUND_A --> INV_A
    DOCUND_A --> CON_A
    ORCH_A --> TAX_A
    ORCH_A --> HC_A
    ORCH_A --> DOM_EVT
    DOM_EVT --> EVT
    EVT --> API_LAYER

    CLASS_A --> IFACE
    INV_A --> IFACE
    CON_A --> IFACE
    TAX_A --> IFACE
    HC_A --> IFACE

    WF --> REPO_IFACE
    ORCH_A --> REPO_IFACE
    ORCH_A --> DOM_SVC

    SK_IMPL -.->|implements| IFACE
    REPO_IMPL -.->|implements| REPO_IFACE
    REPO_IMPL --> DB
    BLOB_IMPL --> FILES
    DOCUND_A --> BLOB_IMPL
```

---

## Frontend Layer

**Technology:** TypeScript, React 18, Vite, Tailwind CSS, shadcn/ui, React Flow, React Query, Zustand

### Responsibilities
- Present the email submission interface
- Display the real-time agent execution graph (React Flow)
- Render human review queue and structured review UI
- Show classification results, extraction summaries, confidence indicators
- Display taxonomy proposals and category management
- Subscribe to SignalR hub and update UI state in response to agent events

### Key Design Decisions
- **Vite** provides fast HMR during development and optimized production builds
- **React Flow** renders the agent collaboration graph as a live directed graph with animated edges
- **shadcn/ui** provides accessible, Tailwind-based UI components with minimal configuration
- **React Query** manages server state (email lists, review queue, taxonomy) with automatic caching and refetch
- **Zustand** manages ephemeral UI state (active workflow, real-time agent events) that doesn't need server persistence
- SignalR client connection is managed in a React context provider, shared across the application

---

## API Layer

**Technology:** ASP.NET Core Minimal APIs (.NET 10), SignalR

### Responsibilities
- Expose REST endpoints for email ingestion, status queries, review decisions, taxonomy management
- Host the SignalR `AgentEventHub` for real-time event streaming
- Map incoming requests to Application layer commands and queries
- Apply request validation, error mapping, and response shaping
- Serve OpenAPI (Swagger) documentation

### Key Design Decisions
- **Minimal APIs** preferred over MVC Controllers: less ceremony, better performance, sufficient for the API surface
- **SignalR Hub** runs in the same process as the API — no separate WebSocket server needed for MVP
- **Endpoint groups** organize endpoints by domain: `/emails`, `/reviews`, `/taxonomy`, `/dashboard`
- **Problem Details (RFC 7807)** for all error responses — consistent, client-parseable error format

---

## Application Layer

**Technology:** C# / .NET 10, no external framework dependency

### Responsibilities
- Coordinate command execution (email ingestion, review decisions, taxonomy approval)
- Handle read queries for dashboard and history views
- Bridge domain events to the SignalR hub
- Define the workflow coordination logic that the Orchestrator Agent implements

### Key Design Decisions
- Application layer has **no dependency on Semantic Kernel or Azure SDKs** — it works through interfaces
- Command/query separation (CQRS-lite) without a full CQRS framework — direct handler registration is sufficient for MVP
- Domain events are raised in the Application layer and dispatched synchronously to the SignalR event bridge

---

## Agent Layer

**Technology:** Microsoft Semantic Kernel, Azure OpenAI (GPT-4o)

### Responsibilities
- Implement the reasoning logic for each specialized agent
- Produce structured outputs with confidence scores and reasoning text
- Invoke the Azure OpenAI API for LLM-backed analysis
- Return typed result objects to the Orchestrator

### Key Design Decisions
- Each agent is a C# class implementing a domain interface (`IClassificationAgent`, `IInvoiceAgent`, etc.)
- Semantic Kernel `Kernel` is injected per-agent for LLM function invocation
- Prompts are stored as `.prompty` files or embedded string templates — not hardcoded in business logic
- Output is deserialized into strongly-typed result records using Semantic Kernel's structured output support

---

## Domain Layer

**Technology:** Pure C# / .NET 10 — no external dependencies

### Responsibilities
- Define domain entities, value objects, and aggregate roots
- Define domain events raised during state transitions
- Define agent and repository interfaces
- Implement domain services for classification conflict resolution, confidence evaluation, and taxonomy matching

### Key Design Decisions
- **Zero external dependencies** — the domain is pure C# business logic
- Domain entities own their state transitions (e.g., `Workflow.Advance()`, `TaxonomyProposal.Approve()`)
- Domain events are plain C# records — no event bus dependency in the domain
- Agent interfaces are defined here so the Application layer can depend on them without knowing the Semantic Kernel implementation

---

## Infrastructure Layer

**Technology:** Semantic Kernel, EF Core 9, Azure SDK, Serilog, OpenTelemetry

### Responsibilities
- Implement agent interfaces using Semantic Kernel + Azure OpenAI
- Implement repository interfaces using EF Core
- Implement attachment storage (local filesystem for MVP; Azure Blob for Phase 2)
- Configure observability (Serilog sinks, OpenTelemetry exporters)
- Manage configuration binding (IOptions pattern; environment variables)

### Key Design Decisions
- Infrastructure registers all implementations in DI without exposing implementation types to upper layers
- EF Core `DbContext` is registered as scoped — safe for per-request lifecycle in the API
- Background processing uses `IHostedService` with a `Channel<WorkflowJob>` — no external queue dependency for MVP
- All Azure SDK clients injected through registered services — replaceable for local development

---

## Persistence Layer

**Technology:** SQLite (MVP), EF Core 9 Migrations

### Responsibilities
- Store all domain entities in a relational schema
- Maintain the immutable audit trail
- Store attachment metadata and blob references
- Support taxonomy versioning and proposal state

### Key Design Decisions
- **EF Core migrations** are the canonical schema management tool — no raw SQL scripts
- **SQLite** requires no installation or configuration for the demo environment
- The `DbContext` provider is configured via `appsettings.json` — switching to PostgreSQL requires one config change and a migration run
- All entities use `Ulid` (lexicographically sortable GUID variant) as primary keys for global uniqueness and pagination friendliness
