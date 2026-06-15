namespace JF.AgenticEnterprise.Application.DTOs;

// ── Agent Conflict ────────────────────────────────────────────────────────────

public record AgentConflictDto(
    string Id,
    string WorkflowId,
    string EmailId,
    string SourceAgent,
    string TargetAgent,
    string ConflictType,
    string Description,
    float SourceConfidence,
    float TargetConfidence,
    string? SourceValue,
    string? TargetValue,
    string? Resolution,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

// ── Workflow Knowledge ────────────────────────────────────────────────────────

public record WorkflowKnowledgeDto(
    string Id,
    string WorkflowId,

    // Phase 1
    string InitialCategory,
    float InitialConfidence,

    // Phase 2
    string? RefinedCategory,
    float? RefinedConfidence,
    string? RefinedReasoning,

    // Phase 3
    string? SuggestedCategory,
    float? SuggestionConfidence,
    string? SuggestionReasoning,

    // Phase 4
    string? ApprovedCategory,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt,

    // Current
    string CurrentCategory,
    float CurrentConfidence,
    string CurrentReasoning,

    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

// ── Human Review ─────────────────────────────────────────────────────────────

public record HumanReviewDto(
    string Id,
    string EmailId,
    string WorkflowId,
    string ReviewType,
    string Priority,
    string Status,
    string Reason,
    float AgentConfidence,
    string? ConflictId,
    string? AssignedTo,
    string? Question,
    string? Recommendation,
    string? Action,
    string? OverrideCategory,
    string? ReviewerNote,
    string? ReviewerId,
    DateTimeOffset QueuedAt,
    DateTimeOffset? OpenedAt,
    DateTimeOffset? DecidedAt);

/// <summary>Request body for POST /reviews/{id}/decide</summary>
public record HumanReviewDecisionRequest(
    /// <summary>One of ReviewAction constants: APPROVE, APPROVE_WITH_CORRECTIONS, REJECT, ESCALATE.</summary>
    string Action,
    string ReviewerId,
    string? ReviewerNote,
    /// <summary>When action is APPROVE_WITH_CORRECTIONS, the corrected category chosen by the human.</summary>
    string? OverrideCategory);

// ── Taxonomy Proposal ─────────────────────────────────────────────────────────

public record TaxonomyProposalDto(
    string Id,
    string SuggestedLabel,
    string Status,
    float Confidence,
    int SampleCount,
    string SuggestedRouting,
    string CreatedByAgent,
    string? WorkflowId,
    string? EmailId,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? DecisionNote,
    DateTimeOffset CreatedAt);

/// <summary>Request body for POST /taxonomy/proposals/{id}/decide</summary>
public record TaxonomyProposalDecisionRequest(
    /// <summary>"APPROVED" or "REJECTED"</summary>
    string Decision,
    string DecidedBy,
    string? DecisionNote);

// ── Reasoning Timeline ────────────────────────────────────────────────────────

public record ReasoningTimelineEntryDto(
    DateTimeOffset Timestamp,
    string EntryType,
    string Actor,
    string Title,
    string Description,
    float? Confidence,
    string? Status,
    string? RelatedId);

public record WorkflowReasoningTimelineDto(
    string WorkflowId,
    IReadOnlyList<ReasoningTimelineEntryDto> Entries);

// ── Extended Workflow Detail (Sprint 3) ───────────────────────────────────────

/// <summary>
/// Extends WorkflowDetailDto with Sprint 3 data.
/// Returned by GET /emails/{id}/workflow when the workflow has Sprint 3 data.
/// </summary>
public record WorkflowDetailExtendedDto(
    string WorkflowId,
    string EmailId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OutcomeType,
    List<WorkflowStepDto> Steps,
    List<AgentExecutionDto> AgentExecutions,
    OrchestrationDecisionDto? OrchestrationDecision,
    WorkflowResultDto? WorkflowResult,

    // Sprint 3 additions
    List<AgentConflictDto> Conflicts,
    WorkflowKnowledgeDto? Knowledge,
    List<HumanReviewDto> HumanReviews,
    List<TaxonomyProposalDto> TaxonomyProposals);

// ── Review queue summary ──────────────────────────────────────────────────────

public record ReviewQueueDto(
    int TotalPending,
    int UrgentCount,
    IReadOnlyList<HumanReviewDto> Reviews);

public record TaxonomyQueueDto(
    int TotalPending,
    IReadOnlyList<TaxonomyProposalDto> Proposals);
