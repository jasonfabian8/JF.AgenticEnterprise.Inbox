namespace JF.AgenticEnterprise.Domain.Entities;

public class HumanReview
{
    public string Id { get; set; } = default!;
    public string EmailId { get; set; } = default!;
    public string WorkflowId { get; set; } = default!;
    public string ReviewType { get; set; } = default!;
    public string Priority { get; set; } = ReviewPriority.Normal;
    public string Status { get; set; } = ReviewStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public float AgentConfidence { get; set; }
    public string? AssignedTo { get; set; }
    public DateTimeOffset QueuedAt { get; set; }
    public DateTimeOffset? OpenedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? Action { get; set; }
    public string CorrectionsJson { get; set; } = "[]";
    public string? ReviewerNote { get; set; }
    public string? ReviewerId { get; set; }
    public int ReviewDurationSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Optional link to the AgentConflict that triggered this review.</summary>
    public string? ConflictId { get; set; }

    /// <summary>
    /// When Action == APPROVE_WITH_CORRECTIONS or a human override, stores the
    /// chosen category or value that overrides all agent conclusions.
    /// </summary>
    public string? OverrideCategory { get; set; }

    public Email Email { get; set; } = default!;
    public Workflow Workflow { get; set; } = default!;
    public AgentConflict? Conflict { get; set; }
}

public static class ReviewStatus
{
    public const string Pending = "PENDING";
    public const string Open = "OPEN";
    public const string Decided = "DECIDED";
    public const string Escalated = "ESCALATED";
    public const string AwaitingInfo = "AWAITING_INFO";
}

public static class ReviewPriority
{
    public const string Urgent = "URGENT";
    public const string Normal = "NORMAL";
    public const string Low = "LOW";
}

public static class ReviewAction
{
    public const string Approve = "APPROVE";
    public const string ApproveWithCorrections = "APPROVE_WITH_CORRECTIONS";
    public const string Reject = "REJECT";
    public const string Escalate = "ESCALATE";
    public const string RequestMoreInfo = "REQUEST_MORE_INFO";
}

public static class ReviewType
{
    public const string ExtractionCorrection = "EXTRACTION_CORRECTION";
    public const string ClassificationOverride = "CLASSIFICATION_OVERRIDE";
    public const string TaxonomyProposal = "TAXONOMY_PROPOSAL";
    public const string ConflictResolution = "CONFLICT_RESOLUTION";
    public const string RiskFlags = "RISK_FLAGS";
    public const string AgentFailure = "AGENT_FAILURE";
}
