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
    float  Confidence,
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
