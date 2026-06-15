namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// Records the Orchestrator Agent's routing decision for a workflow.
/// One decision per workflow execution — determines which specialized agent runs next.
/// </summary>
public class OrchestrationDecision
{
    public string Id { get; set; } = default!;

    public string WorkflowId { get; set; } = default!;

    public string AgentExecutionId { get; set; } = default!;

    /// <summary>The category produced by the Classification Agent that triggered this decision.</summary>
    public string ClassificationCategory { get; set; } = default!;

    /// <summary>Logical name of the next agent to invoke (see <see cref="NextAgentName"/>).</summary>
    public string NextAgent { get; set; } = default!;

    /// <summary>Suggested workflow status after routing (maps to <see cref="WorkflowStatus"/>).</summary>
    public string WorkflowStatus { get; set; } = default!;

    public string Reasoning { get; set; } = string.Empty;

    public DateTimeOffset DecidedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Workflow Workflow { get; set; } = default!;
    public AgentExecution AgentExecution { get; set; } = default!;
}

/// <summary>
/// Well-known values for <see cref="OrchestrationDecision.NextAgent"/>.
/// </summary>
public static class NextAgentName
{
    public const string InvoiceAgent = "InvoiceAgent";
    public const string ContractAgent = "ContractAgent";
    public const string HumanReview = "HumanReview";
    public const string Complete = "Complete";
}
