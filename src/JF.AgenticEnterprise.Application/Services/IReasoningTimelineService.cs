namespace JF.AgenticEnterprise.Application.Services;

/// <summary>
/// Aggregates AgentExecutions, AgentConflicts, TaxonomyProposals, and HumanReviews
/// for a workflow into a single chronological timeline.
/// Used by the Reasoning Timeline frontend component.
/// </summary>
public interface IReasoningTimelineService
{
    Task<IReadOnlyList<ReasoningTimelineEntry>> GetTimelineAsync(
        string workflowId,
        CancellationToken ct = default);
}

public sealed record ReasoningTimelineEntry(
    DateTimeOffset Timestamp,

    /// <summary>"AgentExecution" | "Conflict" | "TaxonomyProposal" | "HumanReview"</summary>
    string EntryType,

    /// <summary>Agent name, "System", or reviewer name.</summary>
    string Actor,

    /// <summary>Short title shown in the timeline card (e.g. "Classification Agent Completed").</summary>
    string Title,

    /// <summary>Full detail text for the expanded view.</summary>
    string Description,

    float? Confidence,

    /// <summary>Status of the entry (e.g. "COMPLETED", "PENDING", "CATEGORY_MISMATCH").</summary>
    string? Status,

    /// <summary>Id of the underlying entity for deep-link navigation.</summary>
    string? RelatedId);
