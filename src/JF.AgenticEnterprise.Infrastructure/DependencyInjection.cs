using JF.AgenticEnterprise.Application.Agents;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Infrastructure.Agents;
using JF.AgenticEnterprise.Infrastructure.Orchestration;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JF.AgenticEnterprise.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── EF Core / SQLite ──────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("InboxDb")
            ?? "Data Source=Data/inbox.db";

        services.AddDbContext<InboxDbContext>(opts =>
            opts.UseSqlite(connectionString));

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<IEmailRepository,          EmailRepository>();
        services.AddScoped<IWorkflowRepository,       WorkflowRepository>();
        services.AddScoped<IClassificationRepository, ClassificationRepository>();
        services.AddScoped<IAgentExecutionRepository, AgentExecutionRepository>();
        services.AddScoped<ITaxonomyRepository,       TaxonomyRepository>();
        services.AddScoped<IAuditRepository,          AuditRepository>();
        services.AddScoped<DataSeeder>();

        // ── AI Provider configuration ─────────────────────────────────────────
        var section   = configuration.GetSection(AiProviderOptions.Section);
        var aiOptions = new AiProviderOptions
        {
            Type                      = section["Type"]                      ?? "AzureAIFoundry",
            ApiKey                    = section["ApiKey"]                    ?? string.Empty,
            ModelId                   = section["ModelId"]                   ?? string.Empty,
            Endpoint                  = section["Endpoint"],
            ClassificationAgentId     = section["ClassificationAgentId"]     ?? "Classification-Agent",
            InvoiceExtractionAgentId  = section["InvoiceExtractionAgentId"]  ?? "Invoice-Agent",
            ContractExtractionAgentId = section["ContractExtractionAgentId"] ?? "Contract-Agent",
            TaxonomyEvolutionAgentId  = section["TaxonomyEvolutionAgentId"]  ?? "Taxonomy-Evolution-Agent",
            HumanCollaborationAgentId = section["HumanCollaborationAgentId"] ?? "Human-Collaboration-Agent",
            OrchestratorAgentId       = section["OrchestratorAgentId"]       ?? "Orchestrator-Agent",
        };
        // Register as singleton so all agents share the same options instance
        services.AddSingleton(aiOptions);

        // ── Agent runtime ─────────────────────────────────────────────────────
        // Singleton: one ChatCompletionsClient = one HTTP connection pool per process.
        services.AddSingleton<IAgentRuntime, AzureAIFoundryAgentRuntime>();

        // ── Agents ────────────────────────────────────────────────────────────
        // Scoped: lightweight; each scope gets fresh logger context.
        services.AddScoped<IClassificationAgent, ClassificationAgent>();

        // ── Orchestration ─────────────────────────────────────────────────────
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

        return services;
    }
}
