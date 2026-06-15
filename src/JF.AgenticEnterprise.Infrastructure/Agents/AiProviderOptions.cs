namespace JF.AgenticEnterprise.Infrastructure.Agents;

public sealed class AiProviderOptions
{
    public const string Section = "AiProvider";

    /// <summary>"OpenAI", "AzureOpenAI", or "AzureAIFoundry"</summary>
    public string Type { get; set; } = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model ID for OpenAI/AzureOpenAI; ignored for AzureAIFoundry (agent IDs used instead).</summary>
    public string ModelId { get; set; } = "gpt-4o-mini";

    /// <summary>Required for AzureOpenAI and AzureAIFoundry.</summary>
    public string? Endpoint { get; set; }

    // ── Foundry agent IDs ─────────────────────────────────────────────────────
    // Configured in appsettings.Development.json (not committed to source control)

    /// <summary>Azure AI Foundry agent ID for email classification.</summary>
    public string ClassificationAgentId { get; set; } = "Classification-Agent";

    /// <summary>Azure AI Foundry agent ID for invoice field extraction (Sprint 2).</summary>
    public string InvoiceExtractionAgentId { get; set; } = "InvoiceExtraction-Agent";

    /// <summary>Azure AI Foundry agent ID for contract field extraction (Sprint 3).</summary>
    public string ContractExtractionAgentId { get; set; } = "Contract-Agent";

    /// <summary>Azure AI Foundry agent ID for taxonomy evolution (Sprint 4).</summary>
    public string TaxonomyEvolutionAgentId { get; set; } = "Taxonomy-Evolution-Agent";

    /// <summary>Azure AI Foundry agent ID for human-in-the-loop collaboration (Sprint 4).</summary>
    public string HumanCollaborationAgentId { get; set; } = "Human-Collaboration-Agent";

    /// <summary>Azure AI Foundry orchestrator agent ID.</summary>
    public string OrchestratorAgentId { get; set; } = "Orchestrator-Agent";
}
