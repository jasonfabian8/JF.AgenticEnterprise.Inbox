namespace JF.AgenticEnterprise.Infrastructure.Agents;

/// <summary>
/// Runtime configuration for the AI inference layer.
/// Secrets (ApiKey) must live in appsettings.Development.json, never in source control.
/// </summary>
public sealed class AiProviderOptions
{
    public const string Section = "AiProvider";

    public string Type { get; set; } = "AzureAIFoundry";

    /// <summary>
    /// Project-scoped endpoint.
    /// Format: https://{resource}.services.ai.azure.com/api/projects/{project-name}
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Underlying model used by all agents (for reference / logging).</summary>
    public string ModelId { get; set; } = "gpt-4.1-mini";

    // ── Prompt Agent names + active versions as deployed in Azure AI Foundry ──
    // Name is passed to AgentReference(name, version).
    // Version targets the specific published snapshot — update when you publish a new version.

    public string ClassificationAgentId      { get; set; } = "Classification-Agent";
    public string ClassificationAgentVersion { get; set; } = "5";

    public string OrchestratorAgentId      { get; set; } = "Orchestrator-Agent";
    public string OrchestratorAgentVersion { get; set; } = "1";

    public string InvoiceAgentId      { get; set; } = "Invoice-Agent";
    public string InvoiceAgentVersion { get; set; } = "1";

    public string ContractAgentId      { get; set; } = "Contract-Agent";
    public string ContractAgentVersion { get; set; } = "1";
}
