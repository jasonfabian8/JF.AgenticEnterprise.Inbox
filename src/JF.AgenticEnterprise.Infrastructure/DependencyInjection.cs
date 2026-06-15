using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Infrastructure.Agents;
using JF.AgenticEnterprise.Infrastructure.Orchestration;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;
using JF.AgenticEnterprise.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        };
        services.AddSingleton(aiOptions);

        // ── Agent runtime ─────────────────────────────────────────────────────
        // Singleton: one ChatCompletionsClient = one HTTP connection pool per process.
        services.AddSingleton<IAgentRuntime, AzureAIFoundryAgentRuntime>();

        // ── Agents (scoped — lightweight, share the singleton runtime) ─────────
        services.AddScoped<IClassificationAgent, ClassificationAgent>();
        services.AddScoped<IOrchestratorAgent, OrchestratorAgent>();
        services.AddScoped<IInvoiceAgent, InvoiceAgent>();
        services.AddScoped<IContractAgent, ContractAgent>();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<IDocumentExtractionService, DocumentExtractionService>();

        // ── Orchestration ─────────────────────────────────────────────────────
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

        return services;
    }
}
