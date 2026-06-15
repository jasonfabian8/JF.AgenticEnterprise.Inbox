namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// Records a detected disagreement between two agents within a workflow execution.
/// Conflicts drive escalation routing: low confidence triggers Taxonomy Evolution Agent;
/// category mismatches trigger Human Collaboration Agent.
/// </summary>
public class AgentConflict
{
    public string Id { get; set; } = default!;
    public string WorkflowId { get; set; } = default!;
    public string EmailId { get; set; } = default!;

    /// <summary>Agent that produced the first/baseline result (e.g. "ClassificationAgent").</summary>
    public string SourceAgent { get; set; } = default!;

    /// <summary>Agent whose output contradicts SourceAgent (e.g. "ContractAgent").</summary>
    public string TargetAgent { get; set; } = default!;

    /// <summary>One of <see cref="ConflictKind"/> constants.</summary>
    public string ConflictType { get; set; } = default!;

    /// <summary>What disagreed and by how much — human readable, surfaced in the UI timeline.</summary>
    public string Description { get; set; } = string.Empty;

    public float SourceConfidence { get; set; }
    public float TargetConfidence { get; set; }

    /// <summary>Source agent's conclusion (e.g. category name, routing decision).</summary>
    public string? SourceValue { get; set; }

    /// <summary>Target agent's contradicting conclusion.</summary>
    public string? TargetValue { get; set; }

    /// <summary>How the conflict was resolved (agent winner, human override, taxonomy update).</summary>
    public string? Resolution { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    // ── Navigation ─────────────────────────────────────────────────────────────
    public Workflow Workflow { get; set; } = default!;
    public Email Email { get; set; } = default!;
    public ICollection<HumanReview> HumanReviews { get; set; } = [];
}

public static class ConflictKind
{
    /// <summary>
    /// Classification Agent and a specialized agent disagree on the document category.
    /// E.g. Classification says "Contract 65%" but Contract Agent concludes "Commercial Proposal 88%".
    /// </summary>
    public const string CategoryMismatch = "CATEGORY_MISMATCH";

    /// <summary>
    /// Agent confidence is below the configured escalation threshold;
    /// no inter-agent disagreement, but the system cannot auto-accept the result.
    /// </summary>
    public const string LowConfidence = "LOW_CONFIDENCE";

    /// <summary>A specialized agent could not locate required fields in the document.</summary>
    public const string MissingInformation = "MISSING_INFORMATION";

    /// <summary>Orchestrator routing decision contradicts the Classification Agent's category.</summary>
    public const string RoutingDispute = "ROUTING_DISPUTE";
}
