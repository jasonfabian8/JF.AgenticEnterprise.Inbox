# Section 08 — Backend Architecture

---

## Project Structure

The backend is a single .NET 10 solution organized into projects that enforce Clean Architecture dependency rules at compile time. Each project boundary is a dependency enforcement boundary.

```
JF.AgenticEnterprise.Inbox.sln
│
├── src/
│   ├── JF.AgenticEnterprise.Inbox.Domain/
│   ├── JF.AgenticEnterprise.Inbox.Application/
│   ├── JF.AgenticEnterprise.Inbox.Infrastructure/
│   └── JF.AgenticEnterprise.Inbox.Api/
│
├── tests/
│   ├── JF.AgenticEnterprise.Inbox.Domain.Tests/
│   ├── JF.AgenticEnterprise.Inbox.Application.Tests/
│   ├── JF.AgenticEnterprise.Inbox.Infrastructure.Tests/
│   └── JF.AgenticEnterprise.Inbox.Api.Tests/
│
└── docs/
    └── sad/
```

---

## Folder Structure — Domain Project

```
JF.AgenticEnterprise.Inbox.Domain/
│
├── Entities/
│   ├── Email.cs
│   ├── Attachment.cs
│   ├── Workflow.cs
│   ├── WorkflowStep.cs
│   ├── AgentExecution.cs
│   ├── Classification.cs
│   ├── InvoiceExtraction.cs
│   ├── LineItem.cs                    ← Value Object
│   ├── ContractExtraction.cs
│   ├── RiskFlag.cs
│   ├── TaxonomyCategory.cs
│   ├── TaxonomyProposal.cs
│   ├── TaxonomyCandidate.cs
│   ├── HumanReview.cs
│   └── AuditEntry.cs
│
├── Events/
│   ├── EmailIngestedEvent.cs
│   ├── WorkflowStartedEvent.cs
│   ├── AgentStartedEvent.cs
│   ├── AgentCompletedEvent.cs
│   ├── AgentFailedEvent.cs
│   ├── ConflictDetectedEvent.cs
│   ├── ConflictResolvedEvent.cs
│   ├── ReviewRequiredEvent.cs
│   ├── ReviewDecidedEvent.cs
│   ├── WorkflowCompletedEvent.cs
│   ├── TaxonomyProposalCreatedEvent.cs
│   └── TaxonomyCategoryCreatedEvent.cs
│
├── Interfaces/
│   ├── Agents/
│   │   ├── IOrchestrator.cs
│   │   ├── IClassificationAgent.cs
│   │   ├── IDocumentUnderstandingAgent.cs
│   │   ├── IInvoiceAgent.cs
│   │   ├── IContractAgent.cs
│   │   ├── ITaxonomyEvolutionAgent.cs
│   │   └── IHumanCollaborationAgent.cs
│   └── Repositories/
│       ├── IEmailRepository.cs
│       ├── IWorkflowRepository.cs
│       ├── IAgentExecutionRepository.cs
│       ├── IClassificationRepository.cs
│       ├── IExtractionRepository.cs
│       ├── ITaxonomyRepository.cs
│       ├── IHumanReviewRepository.cs
│       └── IAuditRepository.cs
│
├── Services/
│   ├── ConflictResolver.cs
│   ├── ConfidenceEvaluator.cs
│   ├── TaxonomyMatcher.cs
│   └── WorkflowStateTransitioner.cs
│
├── Enums/
│   ├── EmailStatus.cs
│   ├── WorkflowStatus.cs
│   ├── AgentType.cs
│   ├── DocumentType.cs
│   ├── ClassificationCategory.cs     ← Seed categories only; runtime from DB
│   ├── ReviewType.cs
│   ├── ReviewPriority.cs
│   └── RiskFlagSeverity.cs
│
└── Common/
    ├── DomainEvent.cs                 ← Base record for all domain events
    ├── Entity.cs                      ← Base class: Id (ULID), CreatedAt
    └── ValueObject.cs
```

---

## Folder Structure — Application Project

```
JF.AgenticEnterprise.Inbox.Application/
│
├── Emails/
│   ├── Commands/
│   │   ├── IngestEmailCommand.cs
│   │   └── IngestEmailCommandHandler.cs
│   └── Queries/
│       ├── GetEmailByIdQuery.cs
│       ├── GetEmailByIdQueryHandler.cs
│       ├── ListEmailsQuery.cs
│       └── ListEmailsQueryHandler.cs
│
├── Reviews/
│   ├── Commands/
│   │   ├── SubmitReviewDecisionCommand.cs
│   │   └── SubmitReviewDecisionCommandHandler.cs
│   └── Queries/
│       ├── GetReviewQueueQuery.cs
│       └── GetReviewQueueQueryHandler.cs
│
├── Taxonomy/
│   ├── Commands/
│   │   ├── ApproveTaxonomyProposalCommand.cs
│   │   ├── ApproveTaxonomyProposalCommandHandler.cs
│   │   ├── DismissTaxonomyProposalCommand.cs
│   │   └── DismissTaxonomyProposalCommandHandler.cs
│   └── Queries/
│       ├── GetTaxonomyCategoriesQuery.cs
│       ├── GetTaxonomyProposalsQuery.cs
│       └── GetTaxonomyCategoriesQueryHandler.cs
│
├── Dashboard/
│   └── Queries/
│       ├── GetDashboardSummaryQuery.cs
│       └── GetDashboardSummaryQueryHandler.cs
│
├── Workflows/
│   ├── WorkflowCoordinator.cs         ← Manages workflow lifecycle from App layer
│   └── WorkflowJobChannel.cs          ← Channel<WorkflowJob> wrapper
│
├── Events/
│   ├── IDomainEventDispatcher.cs
│   └── SignalREventBridge.cs          ← Translates domain events to SignalR messages
│
├── DTOs/
│   ├── EmailDto.cs
│   ├── EmailSummaryDto.cs
│   ├── WorkflowDto.cs
│   ├── AgentExecutionDto.cs
│   ├── ClassificationDto.cs
│   ├── InvoiceExtractionDto.cs
│   ├── ContractExtractionDto.cs
│   ├── RiskFlagDto.cs
│   ├── ReviewTaskDto.cs
│   ├── TaxonomyCategoryDto.cs
│   ├── TaxonomyProposalDto.cs
│   └── DashboardSummaryDto.cs
│
└── Common/
    ├── ICommandHandler.cs
    ├── IQueryHandler.cs
    └── ApplicationException.cs
```

---

## Folder Structure — Infrastructure Project

```
JF.AgenticEnterprise.Inbox.Infrastructure/
│
├── Agents/
│   ├── SemanticKernel/
│   │   ├── AgentKernelFactory.cs      ← Creates Kernel instances per agent type
│   │   ├── PromptTemplateLoader.cs    ← Loads .prompty files
│   │   └── StructuredOutputParser.cs  ← Deserializes SK outputs to typed results
│   ├── OrchestratorAgent.cs
│   ├── ClassificationAgent.cs
│   ├── DocumentUnderstandingAgent.cs
│   ├── InvoiceAgent.cs
│   ├── ContractAgent.cs
│   ├── TaxonomyEvolutionAgent.cs
│   └── HumanCollaborationAgent.cs
│
├── Prompts/
│   ├── classification.prompty
│   ├── document-understanding.prompty
│   ├── invoice-extraction.prompty
│   ├── contract-extraction.prompty
│   ├── contract-risk-flags.prompty
│   ├── taxonomy-clustering.prompty
│   └── taxonomy-proposal.prompty
│
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Migrations/                    ← EF Core migrations
│   ├── Configurations/                ← IEntityTypeConfiguration<T> per entity
│   │   ├── EmailConfiguration.cs
│   │   ├── WorkflowConfiguration.cs
│   │   ├── AgentExecutionConfiguration.cs
│   │   └── ...
│   └── Repositories/
│       ├── EmailRepository.cs
│       ├── WorkflowRepository.cs
│       ├── AgentExecutionRepository.cs
│       ├── ExtractionRepository.cs
│       ├── TaxonomyRepository.cs
│       ├── HumanReviewRepository.cs
│       └── AuditRepository.cs
│
├── Storage/
│   ├── IAttachmentStore.cs
│   ├── LocalAttachmentStore.cs        ← MVP: local filesystem
│   └── AzureBlobAttachmentStore.cs    ← Phase 2: Azure Blob Storage
│
├── Documents/
│   ├── IPdfTextExtractor.cs
│   ├── PdfPigTextExtractor.cs         ← MVP: PdfPig NuGet package
│   └── AzureDocumentIntelligenceExtractor.cs  ← Phase 2
│
├── BackgroundServices/
│   └── WorkflowBackgroundService.cs   ← IHostedService consuming WorkflowJobChannel
│
├── Telemetry/
│   ├── OpenTelemetryConfigurator.cs
│   └── AgentActivitySource.cs         ← ActivitySource for agent trace spans
│
└── DependencyInjection/
    └── InfrastructureServiceRegistration.cs  ← Extension method: AddInfrastructure()
```

---

## Folder Structure — API Project

```
JF.AgenticEnterprise.Inbox.Api/
│
├── Endpoints/
│   ├── EmailEndpoints.cs              ← /api/v1/emails
│   ├── ReviewEndpoints.cs             ← /api/v1/reviews
│   ├── TaxonomyEndpoints.cs           ← /api/v1/taxonomy
│   ├── DashboardEndpoints.cs          ← /api/v1/dashboard
│   └── HealthEndpoints.cs             ← /health, /health/ready
│
├── Hubs/
│   └── AgentEventHub.cs               ← SignalR hub: IAgentEventHub
│
├── Middleware/
│   ├── RequestLoggingMiddleware.cs
│   ├── ErrorHandlingMiddleware.cs     ← Maps exceptions to RFC 7807 responses
│   └── CorrelationIdMiddleware.cs     ← Injects X-Correlation-Id header
│
├── Contracts/
│   ├── Requests/
│   │   ├── IngestEmailRequest.cs
│   │   ├── SubmitReviewDecisionRequest.cs
│   │   └── ApproveTaxonomyProposalRequest.cs
│   └── Responses/
│       ├── IngestEmailResponse.cs
│       ├── EmailDetailResponse.cs
│       ├── EmailSummaryResponse.cs
│       ├── ReviewTaskResponse.cs
│       └── DashboardSummaryResponse.cs
│
├── Mapping/
│   └── ResponseMapper.cs              ← DTO → Response contract mapping
│
└── Program.cs                         ← Minimal API composition root
```

---

## Namespace Strategy

| Layer | Root Namespace |
|-------|----------------|
| Domain | `JF.AgenticEnterprise.Inbox.Domain` |
| Application | `JF.AgenticEnterprise.Inbox.Application` |
| Infrastructure | `JF.AgenticEnterprise.Inbox.Infrastructure` |
| API | `JF.AgenticEnterprise.Inbox.Api` |

Sub-namespaces follow the folder structure. Example: `JF.AgenticEnterprise.Inbox.Infrastructure.Agents` contains all agent implementations.

---

## Dependency Rules

```
Api             → Application, Domain
Application     → Domain
Infrastructure  → Application, Domain
Domain          → (nothing)
```

**Enforcement:** Project references in `.csproj` files enforce this. `Domain.csproj` has no `<ProjectReference>` elements. `Application.csproj` references only `Domain`. `Infrastructure.csproj` references `Application` and `Domain`. `Api.csproj` references `Application`, `Infrastructure` (for DI registration), and `Domain`.

---

## Program.cs Composition Root

The composition root wires together all layers and configures the pipeline. All dependencies are registered here via extension methods to keep `Program.cs` clean.

```
Program.cs structure:

1. Builder phase:
   builder.Services.AddDomain()            ← Domain services (ConflictResolver, etc.)
   builder.Services.AddApplication()       ← Command/Query handlers, WorkflowChannel
   builder.Services.AddInfrastructure()    ← SK agents, EF Core, repositories, storage
   builder.Services.AddApiServices()       ← SignalR, Swagger, CORS, middleware
   builder.Services.AddObservability()     ← Serilog, OpenTelemetry, AppInsights

2. Pipeline phase:
   app.UseErrorHandling()
   app.UseCorrelationId()
   app.UseRequestLogging()
   app.MapEmailEndpoints()
   app.MapReviewEndpoints()
   app.MapTaxonomyEndpoints()
   app.MapDashboardEndpoints()
   app.MapHealthEndpoints()
   app.MapHub<AgentEventHub>("/hubs/agents")
   app.MapOpenApi()
```

---

## Key Technical Decisions

### ULID as Primary Key

All entities use ULID (`Ulid` type in .NET 9+) as primary keys. ULIDs are 128-bit, lexicographically sortable identifiers that are globally unique without a central authority. Benefits over GUID: naturally sortable by creation time (useful for pagination), no sequential integer predictability, globally unique.

### EF Core Configuration Pattern

Each entity has a dedicated `IEntityTypeConfiguration<T>` class in `Infrastructure/Persistence/Configurations/`. This keeps the `AppDbContext.OnModelCreating` method clean and ensures each entity's mapping is independently testable.

### Channel-Based Background Processing

`System.Threading.Channels.Channel<WorkflowJob>` provides a lightweight, in-process async queue. The `WorkflowBackgroundService` is an `IHostedService` that reads from the channel continuously. This avoids an external message queue for MVP while maintaining async decoupling between the HTTP request and agent execution.

### Prompt File Strategy

LLM prompts are stored as `.prompty` files in `Infrastructure/Prompts/`. This separates prompt authoring from code compilation, allows prompt iteration without code changes, and enables a future transition to Prompt Flow or Azure AI Studio for prompt management.
