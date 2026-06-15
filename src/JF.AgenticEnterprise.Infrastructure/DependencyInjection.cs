using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Domain.Settings;
using JF.AgenticEnterprise.Infrastructure.Agents;
using JF.AgenticEnterprise.Infrastructure.Orchestration;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;
using JF.AgenticEnterprise.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JF.AgenticEnterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core / SQLite ──────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("InboxDb")
            ?? "Data Source=Data/inbox.db";

        services.AddDbContext<InboxDbContext>(opts =>
            opts.UseSqlite(connectionString));

        // ── Repositories ──────────────────────────────────────────────────────

        // Sprint 1
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IClassificationRepository, ClassificationRepository>();
        services.AddScoped<IAgentExecutionRepository, AgentExecutionRepository>();
        services.AddScoped<ITaxonomyRepository, TaxonomyRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<DataSeeder>();

        // Sprint 2
        services.AddScoped<IOrchestrationDecisionRepository, OrchestrationDecisionRepository>();
        services.AddScoped<IInvoiceAnalysisRepository, InvoiceAnalysisRepository>();
        services.AddScoped<IContractAnalysisRepository, ContractAnalysisRepository>();
        services.AddScoped<IWorkflowResultRepository, WorkflowResultRepository>();

        // Sprint 3
        services.AddScoped<IAgentConflictRepository, AgentConflictRepository>();
        services.AddScoped<IWorkflowKnowledgeRepository, WorkflowKnowledgeRepository>();
        services.AddScoped<IHumanReviewRepository, HumanReviewRepository>();
        services.AddScoped<ITaxonomyProposalRepository, TaxonomyProposalRepository>();

        // ── AI Provider configuration ─────────────────────────────────────────
        var section = configuration.GetSection(AiProviderOptions.Section);
        var aiOptions = new AiProviderOptions
        {
            Type     = section["Type"]     ?? "AzureAIFoundry",
            Endpoint = section["Endpoint"] ?? string.Empty,
            ModelId  = section["ModelId"]  ?? "gpt-4.1-mini",
            // Prompt Agent names + active versions as deployed in Foundry
            ClassificationAgentId      = section["ClassificationAgentId"]      ?? "Classification-Agent",
            ClassificationAgentVersion = section["ClassificationAgentVersion"] ?? "5",
            OrchestratorAgentId        = section["OrchestratorAgentId"]        ?? "Orchestrator-Agent",
            OrchestratorAgentVersion   = section["OrchestratorAgentVersion"]   ?? "1",
            InvoiceAgentId             = section["InvoiceAgentId"]             ?? "Invoice-Agent",
            InvoiceAgentVersion        = section["InvoiceAgentVersion"]        ?? "1",
            ContractAgentId            = section["ContractAgentId"]            ?? "Contract-Agent",
            ContractAgentVersion       = section["ContractAgentVersion"]       ?? "1",
            // Sprint 3
            TaxonomyEvolutionAgentId      = section["TaxonomyEvolutionAgentId"]      ?? "Taxonomy-Evolution-Agent",
            TaxonomyEvolutionAgentVersion = section["TaxonomyEvolutionAgentVersion"] ?? "1",
            HumanCollaborationAgentId      = section["HumanCollaborationAgentId"]      ?? "Human-Collaboration-Agent",
            HumanCollaborationAgentVersion = section["HumanCollaborationAgentVersion"] ?? "1",
        };
        services.AddSingleton(aiOptions);

        // ── Workflow settings (confidence thresholds) ─────────────────────────
        var ws = configuration.GetSection(WorkflowSettings.Section);
        var workflowSettings = new WorkflowSettings
        {
            HighConfidenceThreshold   = float.TryParse(ws["HighConfidenceThreshold"],   out var h) ? h : 0.85f,
            MediumConfidenceThreshold = float.TryParse(ws["MediumConfidenceThreshold"], out var m) ? m : 0.70f,
            EnableTaxonomyEvolution   = !bool.TryParse(ws["EnableTaxonomyEvolution"],   out var te) || te,
            EnableHumanCollaboration  = !bool.TryParse(ws["EnableHumanCollaboration"],  out var hc) || hc,
        };
        services.AddSingleton(workflowSettings);

        // ── Agent runtime ─────────────────────────────────────────────────────
        // Singleton: one ChatCompletionsClient = one HTTP connection pool per process.
        services.AddSingleton<IAgentRuntime, AzureAIFoundryAgentRuntime>();

        // ── Agents (scoped — lightweight, share the singleton runtime) ─────────
        services.AddScoped<IClassificationAgent, ClassificationAgent>();
        services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();
        services.AddScoped<IInvoiceAgent, InvoiceAgent>();
        services.AddScoped<IContractAgent, ContractAgent>();
        // Sprint 3
        services.AddScoped<ITaxonomyEvolutionAgent, TaxonomyEvolutionAgent>();
        services.AddScoped<IHumanCollaborationAgent, HumanCollaborationAgent>();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();
        // Sprint 3
        services.AddSingleton<IConflictDetectionService, ConflictDetectionService>();
        services.AddScoped<IReasoningTimelineService, ReasoningTimelineService>();

        // ── Orchestration ─────────────────────────────────────────────────────
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddHostedService<WorkflowRecoveryService>();

        return services;
    }
}
