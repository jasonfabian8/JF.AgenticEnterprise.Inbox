# Section 09 — System Architecture

---

## Context Diagram (C4 Level 1)

```mermaid
C4Context
    title Agentic Enterprise Inbox — System Context

    Person(ops, "Operations Analyst", "Reviews, corrects, and manages automated email processing")
    Person(finance, "Finance Analyst", "Reviews extracted invoice data and approves payments")
    Person(contracts, "Contract Admin", "Reviews extracted contract data and manages renewals")
    Person(business, "Business User", "Submits emails and views processing outcomes")

    System(inbox, "Agentic Enterprise Inbox", "AI-powered multi-agent platform that classifies emails, extracts business data from documents, evolves taxonomy, and coordinates human oversight")

    System_Ext(llm, "Azure OpenAI / Claude API", "Foundation LLM powering all agent reasoning and extraction")
    System_Ext(ocr, "Document Intelligence (Azure)", "OCR and document layout analysis for scanned PDFs and images")
    System_Ext(erp, "ERP System (Phase 2)", "Target system for extracted invoice and contract data export")
    System_Ext(teams, "Microsoft Teams (Phase 2)", "Human review notifications channel")
    System_Ext(graph, "Microsoft Graph (Phase 2)", "Live mailbox integration for email ingestion")

    Rel(ops, inbox, "Manages queue, reviews unknown categories, monitors dashboard", "HTTPS")
    Rel(finance, inbox, "Reviews and corrects invoice extraction results", "HTTPS")
    Rel(contracts, inbox, "Reviews contract data and risk flags", "HTTPS")
    Rel(business, inbox, "Submits emails, views outcomes", "HTTPS")

    Rel(inbox, llm, "Agent prompts and reasoning", "HTTPS/REST")
    Rel(inbox, ocr, "Document parsing and OCR", "HTTPS/REST")
    Rel(inbox, erp, "Structured data export (Phase 2)", "HTTPS/REST")
    Rel(inbox, teams, "Review notifications (Phase 2)", "HTTPS/Webhook")
    Rel(graph, inbox, "Live email delivery (Phase 2)", "HTTPS/Webhook")
```

---

## Container Diagram (C4 Level 2)

```mermaid
C4Container
    title Agentic Enterprise Inbox — Container Diagram

    Person(user, "Users", "Analysts, reviewers, business users")

    Container_Boundary(platform, "Agentic Enterprise Inbox") {

        Container(web_ui, "Web Application", "React / Next.js", "Dashboard, email submission, human review UI, taxonomy management, explainability views")

        Container(api_gw, "API Gateway", "ASP.NET Core / FastAPI", "REST API — email ingestion, status, review decisions, taxonomy, dashboard data")

        Container(orchestrator, "Orchestration Service", "C# / Python Worker", "Hosts the Orchestrator Agent; manages workflow state machine; coordinates agent invocations")

        Container(agent_pool, "Agent Pool", "C# / Python Workers", "Hosts Classification, Document Understanding, Invoice, Contract, Taxonomy Evolution, Human Collaboration agents")

        Container(doc_processor, "Document Processing Service", "Python / Azure Function", "MIME parsing, attachment extraction, PDF text extraction, OCR dispatch")

        Container(review_svc, "Human Review Service", "ASP.NET Core", "Manages review queue, structured review task lifecycle, decision capture, feedback routing")

        Container(taxonomy_svc, "Taxonomy Service", "ASP.NET Core", "CRUD for taxonomy categories, proposal management, retroactive reclassification")

        ContainerDb(db, "Primary Database", "PostgreSQL / Azure SQL", "Emails, attachments, agent executions, workflows, taxonomy, human reviews")

        ContainerDb(blob_store, "Blob Storage", "Azure Blob Storage", "Raw email content, attachment binaries, document processing artifacts")

        Container(msg_bus, "Message Bus", "Azure Service Bus", "Async event delivery between services: email.ingested, agent.completed, review.required")

        Container(cache, "Cache", "Redis", "Processing state, taxonomy cache, dashboard aggregations")
    }

    System_Ext(llm_api, "LLM API (Azure OpenAI)", "Foundation model for all agent reasoning")
    System_Ext(ocr_api, "Azure Document Intelligence", "Layout analysis and OCR")

    Rel(user, web_ui, "Interacts via browser", "HTTPS")
    Rel(web_ui, api_gw, "API calls", "HTTPS/REST")
    Rel(api_gw, orchestrator, "Trigger processing job", "Service Bus / HTTP")
    Rel(api_gw, review_svc, "Review queue and decision API", "HTTP")
    Rel(api_gw, taxonomy_svc, "Taxonomy management API", "HTTP")
    Rel(api_gw, db, "Read status, history, dashboard", "SQL")

    Rel(orchestrator, msg_bus, "Publish / consume events", "AMQP")
    Rel(orchestrator, agent_pool, "Invoke agents", "HTTP / gRPC")
    Rel(orchestrator, cache, "Read/write workflow state", "Redis")

    Rel(agent_pool, llm_api, "LLM prompts", "HTTPS")
    Rel(agent_pool, doc_processor, "Request document analysis", "HTTP")
    Rel(agent_pool, taxonomy_svc, "Read taxonomy, propose categories", "HTTP")
    Rel(agent_pool, db, "Write agent execution records", "SQL")

    Rel(doc_processor, blob_store, "Read/write documents", "HTTPS")
    Rel(doc_processor, ocr_api, "OCR requests", "HTTPS")

    Rel(review_svc, msg_bus, "Consume review.required events", "AMQP")
    Rel(review_svc, db, "Read/write review tasks", "SQL")

    Rel(taxonomy_svc, db, "Read/write taxonomy", "SQL")
    Rel(taxonomy_svc, cache, "Invalidate taxonomy cache", "Redis")
```

---

## Agent Interaction Diagram

```mermaid
sequenceDiagram
    participant UI as Web UI
    participant API as API Gateway
    participant ORCH as Orchestrator Agent
    participant CLASS as Classification Agent
    participant DOCUND as Document Understanding Agent
    participant INV as Invoice Agent
    participant CONTR as Contract Agent
    participant TAX as Taxonomy Evolution Agent
    participant HCOL as Human Collaboration Agent
    participant HUMAN as Human Reviewer

    UI->>API: POST /emails/ingest {email content}
    API-->>UI: 202 Accepted {email_id}
    API->>ORCH: TriggerProcessing(email_id)

    Note over ORCH: Workflow starts

    ORCH->>CLASS: Classify(email_subject, email_body)
    CLASS-->>ORCH: {type: "INVOICE", confidence: 0.94, reasoning: "..."}

    ORCH->>DOCUND: AnalyzeAttachments(attachment_ids)
    DOCUND->>DOCUND: Extract text / OCR
    DOCUND-->>ORCH: {document_type: "INVOICE", confidence: 0.97}

    Note over ORCH: Cross-validate: email=INVOICE, doc=INVOICE ✓ No conflict

    ORCH->>INV: ExtractInvoice(document_id)
    INV->>INV: Parse fields, validate math
    INV-->>ORCH: {vendor, amount, due_date, ..., confidence: 0.97, validation: PASS}

    alt Confidence ≥ 0.85 AND Validation PASS
        ORCH->>ORCH: Mark COMPLETED_AUTO
        ORCH-->>API: WorkflowComplete(outcome)
        API-->>UI: EmailProcessed event (WebSocket)
    else Low confidence OR Validation FAIL
        ORCH->>HCOL: CreateReviewTask(email_id, agent_outputs)
        HCOL-->>ORCH: review_task_id
        ORCH->>API: WorkflowPaused — awaiting human review
        HCOL-->>HUMAN: Review notification
        HUMAN->>HCOL: SubmitDecision(corrections, action)
        HCOL->>ORCH: ResumeWorkflow(corrected_data)
        ORCH->>ORCH: Mark COMPLETED_HUMAN_REVIEWED
        ORCH-->>API: WorkflowComplete(outcome)
    end

    Note over CLASS,TAX: Parallel: unknown category detection
    alt Unknown category detected
        CLASS->>TAX: RecordUnknownCandidate(email_id, signals)
        TAX->>TAX: Cluster with previous unknowns
        alt 3 matching unknowns
            TAX->>HCOL: SubmitCategoryProposal(proposal)
            HCOL-->>HUMAN: Category proposal notification
            HUMAN->>HCOL: ApproveCategory(modified_proposal)
            HCOL->>TAX: CreateCategory(approved)
            TAX->>TAX: Retroactive reclassification
        end
    end
```

---

## Conflict Resolution Detail

```mermaid
flowchart TD
    A[Email Ingested] --> B[Classification Agent]
    A --> C[Document Understanding Agent]
    B --> D{Compare outputs}
    C --> D
    D -->|Same type, both high confidence| E[Consensus → Auto Process]
    D -->|Different types, delta > 0.2| F[CONFLICT DETECTED]
    F --> G[Orchestrator weighs evidence]
    G --> H{Document confidence\nhigher than email?}
    H -->|Yes| I[Document type wins\nLog: doc evidence overrides email subject]
    H -->|No| J[Email classification wins\nLog: insufficient doc signal]
    I --> K{Combined confidence\n≥ 0.85?}
    J --> K
    K -->|Yes| E
    K -->|No| L[Escalate to Human Review\nwith conflict explanation]
    L --> M[Human resolves conflict\nand selects correct type]
    M --> N[Selected type proceeds\nto specialist agent]
```

---

## Processing State Machine

```mermaid
stateDiagram-v2
    [*] --> QUEUED : Email ingested

    QUEUED --> PROCESSING : Orchestrator picks up job

    PROCESSING --> CLASSIFYING : Classification Agent invoked
    CLASSIFYING --> ANALYZING_DOCS : Documents routed for analysis
    ANALYZING_DOCS --> EXTRACTING : Specialist agent invoked

    EXTRACTING --> VALIDATING : Extraction complete
    VALIDATING --> COMPLETED_AUTO : All confidence ≥ threshold, no flags
    VALIDATING --> AWAITING_REVIEW : Low confidence OR validation flags OR conflict

    AWAITING_REVIEW --> UNDER_REVIEW : Human opens review task
    UNDER_REVIEW --> COMPLETED_HUMAN : Human submits decision
    UNDER_REVIEW --> ESCALATED : Human escalates to senior reviewer

    ESCALATED --> UNDER_REVIEW : Senior reviewer picks up

    COMPLETED_AUTO --> [*]
    COMPLETED_HUMAN --> [*]

    PROCESSING --> FAILED : Unhandled error
    FAILED --> QUEUED : Manual retry
```

---

## Infrastructure Deployment

```mermaid
graph TB
    subgraph "Client"
        Browser[Web Browser]
    end

    subgraph "Azure — App Tier"
        CDN[Azure CDN\nStatic Web App]
        APIM[API Management]
        AppSvc[App Service\nAPI Gateway]
        Workers[Container Apps\nAgent Workers]
        FuncApp[Azure Functions\nDocument Processor]
    end

    subgraph "Azure — Data Tier"
        SQL[(Azure SQL\nPrimary DB)]
        Blob[(Azure Blob\nDocuments)]
        Redis[(Azure Cache\nfor Redis)]
        SB[Azure Service Bus]
    end

    subgraph "External Services"
        AOAI[Azure OpenAI]
        DocIntel[Azure Document\nIntelligence]
        AppInsights[Application Insights\nMonitoring]
    end

    Browser --> CDN
    CDN --> APIM
    APIM --> AppSvc
    AppSvc --> SB
    AppSvc --> SQL
    AppSvc --> Redis
    SB --> Workers
    Workers --> AOAI
    Workers --> SQL
    Workers --> Redis
    Workers --> FuncApp
    FuncApp --> Blob
    FuncApp --> DocIntel
    Workers --> AppInsights
    AppSvc --> AppInsights
```
