namespace JF.AgenticEnterprise.Domain.Entities;

/// <summary>
/// Aggregate result record for a completed workflow.
/// Created once per workflow when processing finishes (success or human-review).
/// Provides a single query point for the complete processing outcome.
/// </summary>
public class WorkflowResult
{
    public string Id { get; set; } = default!;

    public string WorkflowId { get; set; } = default!;

    // ── Classification summary (always present) ────────────────────────────────

    public string ClassificationCategory { get; set; } = default!;

    public float ClassificationConfidence { get; set; }

    // ── Routing summary ───────────────────────────────────────────────────────

    public string RoutedToAgent { get; set; } = default!;

    // ── Specialized analysis results (mutually exclusive) ─────────────────────

    /// <summary>Set when Invoice Agent ran successfully.</summary>
    public string? InvoiceAnalysisId { get; set; }

    /// <summary>Set when Contract Agent ran successfully.</summary>
    public string? ContractAnalysisId { get; set; }

    // ── Final status ──────────────────────────────────────────────────────────

    /// <summary>Maps to <see cref="WorkflowResultStatus"/>.</summary>
    public string FinalStatus { get; set; } = WorkflowResultStatus.Completed;

    /// <summary>Human-readable summary of what was accomplished.</summary>
    public string Summary { get; set; } = string.Empty;

    public DateTimeOffset CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Workflow Workflow { get; set; } = default!;
    public InvoiceAnalysis? InvoiceAnalysis { get; set; }
    public ContractAnalysis? ContractAnalysis { get; set; }
}

public static class WorkflowResultStatus
{
    public const string Completed = "COMPLETED";
    public const string CompletedExtracted = "COMPLETED_EXTRACTED";
    public const string CompletedHuman = "COMPLETED_HUMAN";
    public const string AwaitingReview = "AWAITING_REVIEW";
    public const string Failed = "FAILED";
}
