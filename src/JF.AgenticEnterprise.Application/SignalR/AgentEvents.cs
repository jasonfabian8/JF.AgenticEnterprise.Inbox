namespace JF.AgenticEnterprise.Application.SignalR;

// ── Agent-level events ────────────────────────────────────────────────────────

public sealed record AgentStartedEvent(
    string WorkflowId,
    string Agent,
    string EmailId,
    DateTimeOffset Timestamp);

public sealed record AgentCompletedEvent(
    string WorkflowId,
    string Agent,
    string EmailId,
    string Category,
    float Confidence,
    string Reasoning,
    DateTimeOffset Timestamp);

public sealed record AgentFailedEvent(
    string WorkflowId,
    string Agent,
    string EmailId,
    string Error,
    DateTimeOffset Timestamp);

// ── Workflow-level events ─────────────────────────────────────────────────────

/// <summary>
/// Emitted each time the workflow advances to a new step (e.g. Classification → Orchestration → Invoice).
/// </summary>
public sealed record WorkflowUpdatedEvent(
    string WorkflowId,
    string EmailId,
    string Status,
    string CurrentStep,
    string? NextAgent,
    DateTimeOffset Timestamp);

/// <summary>
/// Emitted once when the workflow reaches a terminal state (completed, awaiting-review, or failed).
/// </summary>
public sealed record WorkflowCompletedEvent(
    string WorkflowId,
    string EmailId,
    string FinalStatus,
    string ClassificationCategory,
    string RoutedToAgent,
    string? InvoiceAnalysisId,
    string? ContractAnalysisId,
    DateTimeOffset Timestamp);

// ── Sprint 3 — Reasoning / Collaboration events ───────────────────────────────

/// <summary>
/// Emitted when two agents disagree on category or confidence falls below threshold.
/// Frontend: shows the conflict card in the Agent Collaboration View.
/// </summary>
public sealed record ConflictDetectedEvent(
    string WorkflowId,
    string EmailId,
    string ConflictId,
    string ConflictType,
    string SourceAgent,
    string TargetAgent,
    string? SourceValue,
    string? TargetValue,
    float SourceConfidence,
    float TargetConfidence,
    string Description,
    DateTimeOffset Timestamp);

/// <summary>
/// Emitted when Taxonomy-Evolution-Agent suggests a new or different category.
/// Frontend: shows the taxonomy panel notification badge.
/// </summary>
public sealed record TaxonomySuggestedEvent(
    string WorkflowId,
    string EmailId,
    string ProposalId,
    string SuggestedCategory,
    float Confidence,
    string Reasoning,
    DateTimeOffset Timestamp);

/// <summary>
/// Emitted when Human-Collaboration-Agent creates a HumanReview task.
/// Frontend: increments the review queue badge and shows a toast.
/// </summary>
public sealed record ReviewRequestedEvent(
    string WorkflowId,
    string EmailId,
    string ReviewId,
    string ReviewType,
    string Priority,
    string Question,
    string Recommendation,
    DateTimeOffset Timestamp);

/// <summary>
/// Emitted when a human reviewer submits their decision.
/// Frontend: removes the item from the review queue and updates the reasoning timeline.
/// </summary>
public sealed record ReviewCompletedEvent(
    string WorkflowId,
    string EmailId,
    string ReviewId,
    string Action,
    string ReviewerId,
    string? OverrideCategory,
    DateTimeOffset Timestamp);
