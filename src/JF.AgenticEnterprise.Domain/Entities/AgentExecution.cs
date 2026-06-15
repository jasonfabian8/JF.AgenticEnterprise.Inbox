namespace JF.AgenticEnterprise.Domain.Entities;

public class AgentExecution
{
    public string Id { get; set; } = default!;
    public string WorkflowId { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string AgentType { get; set; } = default!;
    public string AgentVersion { get; set; } = "1.0";
    public string Status { get; set; } = "COMPLETED";
    public string? InputPayloadJson { get; set; }
    public string? OutputPayloadJson { get; set; }
    public float ConfidenceScore { get; set; }
    public string ReasoningText { get; set; } = string.Empty;
    public string FlagsJson { get; set; } = "[]";
    public int DurationMs { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Workflow Workflow { get; set; } = default!;
    public Email Email { get; set; } = default!;
}

public static class AgentTypes
{
    public const string Orchestrator        = "OrchestratorAgent";
    public const string Classification      = "ClassificationAgent";
    public const string DocumentUnderstanding = "DocumentUnderstandingAgent";
    public const string Invoice             = "InvoiceAgent";
    public const string Contract            = "ContractAgent";
    public const string TaxonomyEvolution   = "TaxonomyEvolutionAgent";
    public const string HumanCollaboration  = "HumanCollaborationAgent";
}
