# Section 16 — Architectural Decision Records (ADR)

---

## ADR-001 — Use Microsoft Semantic Kernel as the Agent Orchestration Framework

**Date:** 2026-06-14
**Status:** Accepted

### Context

The platform requires a framework to manage LLM interactions, prompt templating, structured output parsing, and agent invocation. Multiple options exist: raw Azure OpenAI SDK, LangChain (Python), Semantic Kernel (.NET), or custom implementation.

### Decision

**Microsoft Semantic Kernel** is selected as the agent orchestration framework.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Raw Azure OpenAI SDK | Full control, no abstraction overhead | No orchestration primitives; significant custom plumbing required |
| LangChain (Python) | Rich ecosystem, extensive documentation | Requires Python backend — contradicts .NET constraint |
| Custom implementation | Perfect fit for our exact needs | Too much development time for hackathon timeline |
| Semantic Kernel (.NET) | Native .NET, Microsoft-backed, designed for enterprise agents | Rapidly evolving API surface; some features still preview |

### Consequences

- **Positive:** Native .NET integration; structured output support; plugin system for agent-as-function; Microsoft-aligned for hackathon category
- **Positive:** `KernelFunction` model maps cleanly to our agent interface pattern
- **Negative:** SK is evolving rapidly — API changes between versions require version pinning
- **Mitigation:** Pin to a specific SK NuGet version; monitor release notes; abstract SK behind domain interfaces so an upgrade doesn't affect Application or Domain layers

---

## ADR-002 — SQLite for MVP Persistence

**Date:** 2026-06-14
**Status:** Accepted

### Context

The MVP needs a database. PostgreSQL and SQL Server are robust choices but require server installation, container orchestration, or cloud provisioning — creating friction for local development, CI, and demo deployment.

### Decision

**SQLite** is used for MVP persistence. EF Core is used as the ORM to ensure the database provider is swappable.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| PostgreSQL | Production-grade, JSONB support, full-featured | Requires Docker or cloud instance; setup friction |
| SQL Server | Familiar to .NET developers, Azure-native | Heavier footprint; licensing considerations |
| SQLite | Zero-install, file-based, EF Core supported | No concurrent writes at scale; no native JSON column type |
| In-memory EF Core | Fastest setup, no files | Not suitable for demo data persistence across restarts |

### Consequences

- **Positive:** Zero setup; database is a file; works on any OS; no Docker required for basic dev
- **Positive:** EF Core migration to PostgreSQL requires only a provider swap and migration run
- **Negative:** SQLite has limited concurrent write throughput — not suitable for production
- **Negative:** No native JSONB — JSON columns stored as TEXT, limiting query capability
- **Mitigation:** EF Core abstraction ensures migration path is clean; PostgreSQL-specific configuration prepared but deferred

---

## ADR-003 — In-Process Channel for Background Workflow Processing (No External Queue)

**Date:** 2026-06-14
**Status:** Accepted

### Context

Email processing is asynchronous. The HTTP ingest endpoint must return immediately (202 Accepted) while agents execute in the background. This requires some form of async job queue. Options include Azure Service Bus, RabbitMQ, in-memory queues, or .NET's `Channel<T>`.

### Decision

**`System.Threading.Channels.Channel<WorkflowJob>`** with an `IHostedService` consumer is used for MVP background processing.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Azure Service Bus | Durable, scales, enterprise-grade | Requires Azure provisioning; adds infrastructure dependency |
| RabbitMQ | Open source, mature, local Docker option | Requires Docker container; additional ops knowledge needed |
| Hangfire | Rich scheduling, dashboard, persistence | Overkill for MVP; additional dependency |
| Channel<T> | Zero infrastructure; .NET native; sufficient for single-instance | Not durable across restarts; not scalable beyond one instance |

### Consequences

- **Positive:** No external dependencies; single deployment unit; simple to reason about
- **Positive:** `Channel<T>` is high-performance and supports backpressure natively
- **Negative:** Jobs in-flight when the application restarts are lost
- **Negative:** Cannot scale to multiple API instances without a real queue
- **Mitigation:** For demo, in-flight job loss is acceptable. Phase 2 replaces `Channel<T>` with Azure Service Bus — the `IHostedService` consumer is replaced by a Service Bus trigger; the `WorkflowCoordinator` remains unchanged

---

## ADR-004 — SignalR for Real-Time Agent Event Streaming

**Date:** 2026-06-14
**Status:** Accepted

### Context

The demo requires that agent execution events appear in the UI in real time. Options include WebSocket (raw), Server-Sent Events (SSE), polling, or SignalR.

### Decision

**ASP.NET Core SignalR** is used for all real-time server-to-client communication.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Raw WebSocket | Maximum control | Complex connection management; no group/hub abstractions |
| Server-Sent Events (SSE) | Simple, HTTP-based, one-way | No group management; harder to implement with React |
| Long Polling | Universal compatibility | High latency; excessive HTTP overhead |
| SignalR | .NET-native; automatic transport negotiation; hub groups | Requires WebSocket enabled on hosting; Azure SignalR Service for scale |

### Consequences

- **Positive:** First-class .NET support; typed hub interfaces; automatic fallback transports
- **Positive:** Group management maps cleanly to "workflow-specific event channels"
- **Positive:** `@microsoft/signalr` React client is well-maintained and documented
- **Negative:** Requires WebSocket support on the host (enabled on Azure App Service by configuration)
- **Negative:** Multi-instance scaling requires Azure SignalR Service backplane (Phase 2)

---

## ADR-005 — React Flow for Agent Workflow Visualization

**Date:** 2026-06-14
**Status:** Accepted

### Context

A core demo requirement is a live visual representation of agent collaboration — nodes for agents, animated edges for communication, color-coded status. This is the primary UI differentiator.

### Decision

**React Flow** (`@xyflow/react`) is used for agent workflow graph rendering.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| D3.js | Extremely powerful, full control | High implementation complexity; not React-native |
| Cytoscape.js | Graph-specific, mature | Less React-friendly; heavier bundle |
| Custom SVG | Perfect fit | Expensive to build; animation complexity |
| Mermaid.js | Simple, text-based | Not interactive; no real-time updates |
| React Flow | React-native, interactive, customizable nodes/edges, animated edges | Bundle size; some complexity for custom node types |

### Consequences

- **Positive:** Custom node components (React components rendered as graph nodes) enable rich per-agent status display
- **Positive:** Built-in edge animation CSS classes make active agent connections visually compelling
- **Positive:** Excellent documentation and active community
- **Negative:** Bundle adds ~200KB to production build; acceptable for an enterprise tool
- **Negative:** Requires careful state management to drive graph updates from SignalR events

---

## ADR-006 — Feature-Sliced Frontend Organization

**Date:** 2026-06-14
**Status:** Accepted

### Context

The frontend has multiple distinct functional areas (inbox, workflow visualization, review queue, taxonomy management, dashboard). Standard approaches include organizing by type (components/, hooks/, pages/) or by feature.

### Decision

**Feature-based organization** is used, where each feature folder contains all components, hooks, and types for that feature. Shared code lives in top-level `lib/` and `components/` folders.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| By type (components/, hooks/, pages/) | Simple, familiar | Features become scattered across folders; hard to navigate |
| Feature Sliced Design (full FSD) | Very structured, scalable | Overhead for small team; adds concepts (segments, slices, layers) |
| Feature folders (adopted) | Clear ownership; easy to find related code; scales well | Some cross-feature sharing requires discipline |

### Consequences

- **Positive:** Each feature is a self-contained unit — new features don't require touching existing folders
- **Positive:** Onboarding: "where is the review queue?" → `src/features/review/`
- **Negative:** Some shared types and components require judgment calls on placement

---

## ADR-007 — Clean Architecture with Project-Level Enforcement

**Date:** 2026-06-14
**Status:** Accepted

### Context

In a system where AI providers, databases, and communication protocols may all change over time, protecting core business logic from external dependencies is critical. The choice is between a monolithic project structure or a multi-project Clean Architecture layout.

### Decision

**Clean Architecture with four projects** enforces dependency rules at the compiler level.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Single project with folder structure | Simpler setup | No compiler-enforced boundaries; easy to accidentally add dependencies |
| Vertical Slice Architecture | Each feature is fully self-contained | Less suited for cross-cutting agent coordination logic |
| Clean Architecture (adopted) | Enforced boundaries; easy to test domain in isolation | More projects to manage; more initial setup |

### Consequences

- **Positive:** Adding a new agent or switching from SQLite to PostgreSQL requires no Domain or Application changes
- **Positive:** Domain and Application layers are 100% unit testable without any infrastructure dependencies
- **Negative:** More project files to maintain; developers must understand which layer owns what

---

## ADR-008 — ULID as Primary Key Strategy

**Date:** 2026-06-14
**Status:** Accepted

### Context

Entities need globally unique identifiers. Options include auto-increment integers, GUID (random), GUID (sequential), or ULID.

### Decision

**ULID (Universally Unique Lexicographically Sortable Identifier)** is used for all entity primary keys.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Auto-increment int | Simple, small storage | Sequential prediction; not globally unique; bad for distributed |
| Random GUID | Globally unique | Random ordering causes index fragmentation; not sortable |
| Sequential GUID (newid() in SQL) | Sortable in SQL Server | SQL Server-specific; not cross-provider |
| ULID (adopted) | Globally unique + lexicographically sortable; 128-bit | Less familiar than GUID; not natively in all ORMs |

### Consequences

- **Positive:** IDs sort by creation time without a separate `CreatedAt` index for most use cases
- **Positive:** Globally unique without database sequence — safe for future distributed architecture
- **Positive:** .NET `Ulid` type available natively in .NET 9+
- **Negative:** Less familiar; requires education for new team members

---

## ADR-009 — Taxonomy Stored as Runtime Data (Not Hardcoded Enum)

**Date:** 2026-06-14
**Status:** Accepted

### Context

The platform's core learning capability requires that email categories can be added at runtime without a deployment. This fundamentally determines whether taxonomy is data or code.

### Decision

The taxonomy is a **runtime data model** stored in the `TaxonomyCategories` table. Seed categories are loaded via EF Core data seeding.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| C# enum + switch statement | Simple, compile-time checked | Cannot be extended at runtime; Taxonomy Evolution Agent feature is impossible |
| Configuration file (appsettings.json) | No code change needed | Requires restart to take effect; hard to edit through UI |
| Database table (adopted) | Runtime extensible; UI-manageable; version-trackable | Slightly more complex classification prompt construction |

### Consequences

- **Positive:** Taxonomy Evolution Agent can add new categories at runtime without any deployment
- **Positive:** Human approvals take effect immediately
- **Positive:** Retroactive reclassification is feasible — just re-run classification with updated taxonomy
- **Negative:** Classification Agent prompt must be built dynamically at invocation time (minor performance cost)
- **Mitigation:** Taxonomy is read once at workflow start and cached in the `EmailProcessingContext` for the duration of the workflow

---

## ADR-010 — Prompts as External Files (.prompty)

**Date:** 2026-06-14
**Status:** Accepted

### Context

LLM prompts are a core part of the system's behavior. They need to be iterable independently of code, reviewable by non-developers, and potentially managed in Azure AI Studio in the future.

### Decision

Prompts are stored as **`.prompty` files** in the Infrastructure project's `Prompts/` directory.

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Hardcoded C# strings | Simplest; no file I/O | Prompts buried in code; requires recompile to iterate; not reviewable |
| Database-stored prompts | Runtime-editable; versioned | Adds UI complexity; DB dependency for core agent logic |
| Embedded string resources | Compile-time included | Still requires recompile; harder to read in context |
| .prompty files (adopted) | Standard format; Semantic Kernel native support; readable outside IDE | File path management; must be included in build output |

### Consequences

- **Positive:** Prompt engineers can edit prompts without touching C# code
- **Positive:** Prompts are visible in version control diffs — prompt changes are auditable
- **Positive:** `.prompty` format is natively supported by Semantic Kernel and VS Code extension
- **Negative:** Files must be marked `CopyToOutputDirectory` in the project file
- **Mitigation:** CI build verifies that all expected `.prompty` files are present in the output directory

---

## ADR-011 — Human Review as a First-Class Agent

**Date:** 2026-06-14
**Status:** Accepted

### Context

Human review can be implemented as either a side-channel (the workflow sends a notification and waits for a webhook) or as a first-class agent that participates in the same orchestration model as AI agents.

### Decision

The **Human Collaboration Agent** is a first-class agent in the Semantic Kernel agent pool, with the same interface contract as AI agents. Its "execution" creates a review task and suspends the workflow — the human response is the agent's "completion."

### Alternatives Considered

| Option | Pros | Cons |
|--------|------|------|
| Side-channel notification + webhook | Simple to implement | Human review is architecturally invisible; breaks agent model consistency |
| Separate review service | Clean separation | Requires service-to-service communication; more infrastructure |
| First-class agent (adopted) | Consistent model; human and AI agents are peers; uniform audit trail | Workflow must handle suspension gracefully |

### Consequences

- **Positive:** The Orchestrator treats human review identically to any other agent invocation — uniform code path
- **Positive:** Human decisions appear in the same `AgentExecution` audit trail as AI decisions
- **Positive:** The agent graph visualization shows the Human Collaboration Agent as a node — humans are literally visible in the collaboration graph
- **Negative:** Workflow state machine must support a SUSPENDED state and a resume mechanism via HTTP callback
