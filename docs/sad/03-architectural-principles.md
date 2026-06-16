# Section 03 — Architectural Principles

---

## Principle 1 — Agent Isolation with Orchestrated Collaboration

**Statement:** Each agent is a self-contained unit of intelligence with a single, well-defined responsibility. Agents do not call each other directly. All inter-agent communication flows through the Orchestrator Agent.

**Rationale:** Direct agent-to-agent coupling creates a web of dependencies that is difficult to test, debug, and extend. Centralizing communication through the Orchestrator makes the workflow explicit, auditable, and controllable. It also enables conflict detection — the Orchestrator can compare outputs from multiple agents before deciding the next action.

**Design Impact:**
- Each agent is implemented as a Semantic Kernel `KernelFunction` or `IChatCompletionService` wrapper with a defined input/output contract
- The Orchestrator holds the workflow state machine and is the only entity that can invoke other agents
- Agent-to-agent communication is modeled as structured data transfer through the Orchestrator, never as direct method calls or events between agents
- Testing each agent in isolation (unit) and in collaboration (integration) is a first-class concern

---

## Principle 2 — Explainability as a First-Class Architectural Concern

**Statement:** Reasoning, confidence scores, and decision rationale are mandatory outputs of every agent execution — not optional metadata. The system architecture ensures these are produced, stored, and surfaced to users at every level.

**Rationale:** An AI system that cannot explain its decisions will not be adopted by knowledge workers, will not satisfy compliance requirements, and will not demonstrate the Reasoning Agents judging criterion. Explainability must be baked into the agent contracts, the data model, and the UI — not added as an afterthought.

**Design Impact:**
- Every agent interface requires `ReasoningText` and `ConfidenceScore` in its output type
- The `AgentExecution` entity is a first-class domain entity, not a log entry
- The frontend's agent visualization is a primary feature, not a debug tool
- Confidence scores drive routing decisions programmatically — they are functional, not cosmetic
- The reasoning chain is queryable and exportable, not only viewable in the UI

---

## Principle 3 — Clean Architecture with Enforced Dependency Direction

**Statement:** The backend follows Clean Architecture. Dependencies point inward: Infrastructure and Presentation depend on Application, which depends on Domain. Domain depends on nothing external.

**Rationale:** In a system where the AI provider (Azure OpenAI), the database engine (SQLite → PostgreSQL), and the communication protocols (REST, SignalR) are all subject to change or evolution, inversion of control and dependency abstraction protect the core domain logic from external change.

**Design Impact:**
- Domain layer has zero references to Semantic Kernel, EF Core, Azure SDKs, or HTTP
- Application layer defines agent interfaces (`IClassificationAgent`, `IInvoiceAgent`) as abstractions
- Infrastructure layer contains all Semantic Kernel implementations, EF Core repositories, and Azure SDK integrations
- A single EF Core migration converts SQLite to PostgreSQL without touching Domain or Application
- Agent implementations can be swapped (e.g., from Azure OpenAI to a local model) by replacing Infrastructure, not Application or Domain

---

## Principle 4 — Async-First, Event-Driven Agent Execution

**Statement:** Agent executions are inherently asynchronous. The system does not block HTTP request threads during LLM inference. Real-time updates are pushed to the client via SignalR events, not polled.

**Rationale:** LLM inference takes 2–15 seconds per agent call. Synchronous HTTP processing would create unacceptable latency, exhaust thread pool resources, and produce a poor user experience. An async, event-driven model allows the UI to show real-time progress while the server processes without blocking.

**Design Impact:**
- Email processing is initiated via a POST that returns 202 Accepted immediately
- The workflow executes in a background context (IHostedService / Channel-based queue)
- Each agent completion publishes a domain event captured and forwarded to SignalR
- The frontend subscribes to SignalR hub and receives agent events as they occur, updating the React Flow graph live
- Cancellation tokens are passed through the entire agent execution chain

---

## Principle 5 — Taxonomy as a Runtime Data Model

**Statement:** The classification taxonomy is not hardcoded. Categories, signals, routing rules, and extraction field mappings are stored as data and read at runtime. The taxonomy can be extended without code changes.

**Rationale:** The Taxonomy Evolution Agent's core value proposition is that it can grow the system's knowledge without a deployment cycle. This is only possible if the taxonomy is a data artifact, not a compiled enum or switch statement. This principle is what makes the platform adaptive.

**Design Impact:**
- `TaxonomyCategory` is a persisted entity, not an application constant
- The Classification Agent receives the current taxonomy at runtime as part of its prompt context
- New categories proposed by the Taxonomy Evolution Agent and approved by humans take effect immediately
- The Orchestrator reads taxonomy routing rules from the database to determine which specialist agent to invoke
- Category signals (keywords, patterns, LLM descriptions) are stored as JSON columns in the taxonomy entity

---

## Principle 6 — Simplicity at the MVP Boundary

**Statement:** For the hackathon MVP, the simplest working implementation is preferred over the most elegant future-proof design. Complexity is added only when it directly enables a required demo capability.

**Rationale:** Over-engineering an MVP is a common failure mode in hackathon projects. The goal is a compelling, working demonstration — not a production system. Architectural decisions that add complexity without adding demo value are deferred. However, simplifications are made consciously with documented migration paths.

**Design Impact:**
- SQLite instead of PostgreSQL eliminates database infrastructure for the demo environment
- No message queue (RabbitMQ, Azure Service Bus) for MVP — in-process Channel<T> provides async decoupling sufficient for demo
- Single deployable unit (no microservices) reduces DevOps overhead to zero
- No authentication middleware in MVP — placeholder middleware with documented Phase 2 implementation
- React Query for server state without Redux — sufficient state management for demo complexity
- Simplifications are documented in Section 17 (MVP Architecture) with explicit Phase 2 migration paths

---

## Principle Summary

| Principle | Pattern Enforced | Deferred Until |
|-----------|-----------------|----------------|
| Agent Isolation | Orchestrator-mediated communication | — (core design) |
| Explainability First | Mandatory agent output contracts | — (core design) |
| Clean Architecture | Dependency inversion layers | — (core design) |
| Async-First | 202/SignalR pattern | — (core design) |
| Taxonomy as Data | Runtime taxonomy queries | — (core design) |
| Simplicity at MVP | SQLite, in-process queue, no auth | Phase 2 migration documented |
