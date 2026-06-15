using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;
using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Infrastructure.Services;

/// <summary>
/// Aggregates AgentExecutions, AgentConflicts, TaxonomyProposals, and HumanReviews
/// for a workflow into a single chronological timeline.
/// </summary>
public sealed class ReasoningTimelineService : IReasoningTimelineService
{
    private readonly IAgentExecutionRepository _executionRepo;
    private readonly IAgentConflictRepository _conflictRepo;
    private readonly ITaxonomyProposalRepository _taxonomyRepo;
    private readonly IHumanReviewRepository _reviewRepo;

    public ReasoningTimelineService(
        IAgentExecutionRepository executionRepo,
        IAgentConflictRepository conflictRepo,
        ITaxonomyProposalRepository taxonomyRepo,
        IHumanReviewRepository reviewRepo)
    {
        _executionRepo = executionRepo;
        _conflictRepo  = conflictRepo;
        _taxonomyRepo  = taxonomyRepo;
        _reviewRepo    = reviewRepo;
    }

    public async Task<IReadOnlyList<ReasoningTimelineEntry>> GetTimelineAsync(
        string workflowId, CancellationToken ct = default)
    {
        var executions = await _executionRepo.GetByWorkflowIdAsync(workflowId, ct);
        var conflicts  = await _conflictRepo.GetByWorkflowIdAsync(workflowId, ct);
        var proposals  = await _taxonomyRepo.GetByWorkflowIdAsync(workflowId, ct);
        var reviews    = await _reviewRepo.GetByWorkflowIdAsync(workflowId, ct);

        var entries = new List<ReasoningTimelineEntry>();

        foreach (var ex in executions)
        {
            var isCompleted = ex.Status == AgentExecutionStatus.Completed;
            var isFailed    = ex.Status == AgentExecutionStatus.Failed;

            entries.Add(new ReasoningTimelineEntry(
                Timestamp:   ex.StartedAt,
                EntryType:   "AgentExecution",
                Actor:       ex.AgentType,
                Title:       $"{ex.AgentType} {(isCompleted ? "Completed" : isFailed ? "Failed" : "Started")}",
                Description: ex.ErrorMessage
                    ?? (ex.ReasoningText.Length > 0 ? ex.ReasoningText : "No output available"),
                Confidence:  isCompleted ? ex.ConfidenceScore : null,
                Status:      ex.Status,
                RelatedId:   ex.Id));
        }

        foreach (var c in conflicts)
        {
            entries.Add(new ReasoningTimelineEntry(
                Timestamp:   c.CreatedAt,
                EntryType:   "Conflict",
                Actor:       "System",
                Title:       $"Conflict Detected — {FormatConflictType(c.ConflictType)}",
                Description: c.Description,
                Confidence:  null,
                Status:      c.ResolvedAt.HasValue ? "RESOLVED" : "OPEN",
                RelatedId:   c.Id));

            if (c.ResolvedAt.HasValue)
            {
                entries.Add(new ReasoningTimelineEntry(
                    Timestamp:   c.ResolvedAt.Value,
                    EntryType:   "Conflict",
                    Actor:       "System",
                    Title:       "Conflict Resolved",
                    Description: c.Resolution ?? "Resolution recorded.",
                    Confidence:  null,
                    Status:      "RESOLVED",
                    RelatedId:   c.Id));
            }
        }

        foreach (var p in proposals)
        {
            entries.Add(new ReasoningTimelineEntry(
                Timestamp:   p.CreatedAt,
                EntryType:   "TaxonomyProposal",
                Actor:       p.CreatedByAgent,
                Title:       $"New Category Suggested: \"{p.SuggestedLabel}\"",
                Description: $"Confidence: {p.Confidence:P0}. Status: {p.Status}.",
                Confidence:  p.Confidence,
                Status:      p.Status,
                RelatedId:   p.Id));
        }

        foreach (var r in reviews)
        {
            entries.Add(new ReasoningTimelineEntry(
                Timestamp:   r.QueuedAt,
                EntryType:   "HumanReview",
                Actor:       "Human-Collaboration-Agent",
                Title:       $"Review Requested — {FormatReviewType(r.ReviewType)} [{r.Priority}]",
                Description: r.Reason,
                Confidence:  r.AgentConfidence,
                Status:      r.Status,
                RelatedId:   r.Id));

            if (r.DecidedAt.HasValue)
            {
                entries.Add(new ReasoningTimelineEntry(
                    Timestamp:   r.DecidedAt.Value,
                    EntryType:   "HumanReview",
                    Actor:       r.ReviewerId ?? "Reviewer",
                    Title:       $"Review Decided — {r.Action}",
                    Description: r.ReviewerNote ?? $"Reviewer {r.ReviewerId} submitted decision: {r.Action}.",
                    Confidence:  null,
                    Status:      r.Action,
                    RelatedId:   r.Id));
            }
        }

        return entries.OrderBy(e => e.Timestamp).ToList();
    }

    private static string FormatConflictType(string type) => type switch
    {
        ConflictKind.CategoryMismatch   => "Category Mismatch",
        ConflictKind.LowConfidence      => "Low Confidence",
        ConflictKind.MissingInformation => "Missing Information",
        ConflictKind.RoutingDispute     => "Routing Dispute",
        _                               => type,
    };

    private static string FormatReviewType(string type) => type switch
    {
        ReviewType.ClassificationOverride => "Classification Override",
        ReviewType.TaxonomyProposal       => "Taxonomy Proposal",
        ReviewType.ConflictResolution     => "Conflict Resolution",
        ReviewType.ExtractionCorrection   => "Extraction Correction",
        ReviewType.RiskFlags              => "Risk Flags",
        ReviewType.AgentFailure           => "Agent Failure",
        _                                 => type,
    };
}
