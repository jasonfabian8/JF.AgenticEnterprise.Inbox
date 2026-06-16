# Section 04 — Solution Overview

---

## Context Diagram

The context diagram shows the Agentic Enterprise Inbox system in relation to its users and external dependencies.

```mermaid
C4Context
    title Agentic Enterprise Inbox — System Context

    Person(analyst, "Business Analyst / Reviewer", "Reviews agent outputs, approves taxonomy proposals, corrects uncertain extractions")
    Person(submitter, "Email Submitter", "Pastes or uploads email content via web UI to trigger processing")

    System(aei, "Agentic Enterprise Inbox", "Multi-agent AI platform that classifies emails, extracts business data from documents, evolves taxonomy, and coordinates human oversight with real-time visibility")

    System_Ext(aoai, "Azure OpenAI", "GPT-4o deployment — provides LLM reasoning for all agents")
    System_Ext(docint, "Azure Document Intelligence (Phase 2)", "OCR and layout analysis for scanned documents")
    System_Ext(graph, "Microsoft Graph (Phase 2)", "Live mailbox integration for automatic email ingestion")
    System_Ext(teams, "Microsoft Teams (Phase 2)", "Human review notifications for mobile/chat workflows")

    Rel(submitter, aei, "Submits emails, views processing status", "HTTPS / Browser")
    Rel(analyst, aei, "Reviews queue, corrects data, approves taxonomy", "HTTPS / Browser")
    Rel(aei, aoai, "Agent reasoning, classification, extraction prompts", "HTTPS / REST")
    Rel(aei, docint, "OCR requests for scanned attachments (Phase 2)", "HTTPS / REST")
    Rel(graph, aei, "Delivers new emails automatically (Phase 2)", "HTTPS / Webhook")
    Rel(aei, teams, "Sends review notifications (Phase 2)", "HTTPS / Bot Framework")
```

---

## System Overview Diagram

The overview shows the major system components and how they interact at the deployment level.

```mermaid
graph TB
    subgraph "Client — Browser"
        UI[React SPA\nTypeScript · Vite · Tailwind\nshadcn/ui · React Flow]
    end

    subgraph "Server — Azure App Service (.NET 10)"
        direction TB
        API[ASP.NET Core\nMinimal APIs\nREST Endpoints]
        HUB[SignalR Hub\nAgentEventHub]
        ORCH[Orchestration Engine\nSemantic Kernel\nOrchestrator Agent]

        subgraph "Agent Pool"
            direction LR
            CL[Classification\nAgent]
            DU[Document\nUnderstanding Agent]
            INV[Invoice\nAgent]
            CON[Contract\nAgent]
            TAX[Taxonomy\nEvolution Agent]
            HC[Human\nCollab Agent]
        end

        BG[Background\nWorkflow Processor\nChannel T]
        EF[EF Core\nRepository Layer]
    end

    subgraph "Persistence"
        DB[(SQLite MVP\nPostgreSQL Phase 2)]
        BLOB[Local Blob Store\nAzure Blob Phase 2]
    end

    subgraph "Azure External Services"
        AOAI[Azure OpenAI\nGPT-4o]
    end

    UI -->|REST| API
    UI <-->|WebSocket / SignalR| HUB
    API --> BG
    API --> EF
    BG --> ORCH
    ORCH --> CL
    ORCH --> DU
    DU --> INV
    DU --> CON
    ORCH --> TAX
    ORCH --> HC
    ORCH --> HUB
    CL --> AOAI
    DU --> AOAI
    INV --> AOAI
    CON --> AOAI
    TAX --> AOAI
    HC --> HUB
    ORCH --> EF
    EF --> DB
    DU --> BLOB
    BG --> HUB
```

---

## Processing Flow Overview

The following diagram illustrates the high-level processing flow for a single email from ingestion to outcome.

```mermaid
flowchart TD
    A([Email Submitted\nvia Web UI]) --> B[POST /api/v1/emails/ingest]
    B --> C[202 Accepted\nemail_id returned]
    C --> D[Email queued in\nin-process Channel]
    D --> E[Background Processor\npicks up job]
    E --> F[Orchestrator Agent\nstarts workflow]

    F --> G[Classification Agent\nanalyzes email text]
    F --> H[Document Understanding Agent\nanalyzes attachments]

    G --> I{Outputs\ncross-validated}
    H --> I

    I -->|Conflict detected| J[Orchestrator resolves\nconflict via evidence weight]
    I -->|Consensus| K[Specialist Agent\ninvoked by category]
    J --> K

    K --> L{Confidence\n≥ threshold?}
    L -->|Yes| M[COMPLETED_AUTO\nresult stored]
    L -->|No| N[Human Collaboration Agent\ncreates review task]
    N --> O[SignalR: review.required\nevent pushed to UI]
    M --> P[SignalR: workflow.completed\nevent pushed to UI]

    O --> Q[Human Reviewer\nacts in UI]
    Q --> R[COMPLETED_HUMAN\nresult stored]
    R --> P

    P --> S([Dashboard updated\nEmail detail available])

    style A fill:#e8f4fd,stroke:#2196F3
    style S fill:#e8f5e9,stroke:#4CAF50
    style N fill:#fff3e0,stroke:#FF9800
    style J fill:#fce4ec,stroke:#F44336
```

---

## Key Architectural Patterns

| Pattern | Where Used | Why |
|---------|-----------|-----|
| Mediator (via Orchestrator) | Agent coordination | Decouples agents; enables conflict detection |
| Strategy | Agent selection by taxonomy type | Open/closed principle — new agents add, don't modify |
| Repository | Data access abstraction | Enables SQLite → PostgreSQL migration transparently |
| Domain Events | Agent execution → SignalR | Decouples agent logic from UI update mechanism |
| CQRS (lightweight) | Read queries vs. command processing | Dashboard reads optimized separately from processing writes |
| Chain of Responsibility | Agent execution sequence | Each agent receives previous context; builds on prior outputs |
| Observer | SignalR hub subscriptions | Multiple clients observe the same workflow progression |
