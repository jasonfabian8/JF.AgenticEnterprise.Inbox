namespace JF.AgenticEnterprise.Domain.Events;

public record EmailIngestedEvent(
    string EmailId,
    string Source,
    string SenderEmail,
    string Subject,
    int AttachmentCount,
    DateTimeOffset OccurredAt);

public record WorkflowStartedEvent(
    string WorkflowId,
    string EmailId,
    DateTimeOffset StartedAt);

public record AgentStartedEvent(
    string WorkflowId,
    string EmailId,
    string AgentType,
    int StepOrder,
    DateTimeOffset StartedAt);

public record AgentCompletedEvent(
    string WorkflowId,
    string EmailId,
    string AgentType,
    string Status,
    float ConfidenceScore,
    string ReasoningText,
    string[] Flags,
    int DurationMs,
    DateTimeOffset CompletedAt);

public record AgentFailedEvent(
    string WorkflowId,
    string EmailId,
    string AgentType,
    string Status,
    string ErrorMessage,
    int DurationMs,
    DateTimeOffset FailedAt);

public record ConflictDetectedEvent(
    string WorkflowId,
    string EmailId,
    string EmailClassificationType,
    float EmailClassificationConfidence,
    string DocumentType,
    float DocumentConfidence,
    DateTimeOffset DetectedAt);

public record ConflictResolvedEvent(
    string WorkflowId,
    string EmailId,
    string Winner,
    string WinnerType,
    string ResolutionReasoning,
    DateTimeOffset ResolvedAt);

public record ReviewRequiredEvent(
    string ReviewId,
    string EmailId,
    string WorkflowId,
    string ReviewType,
    string Priority,
    string Reason,
    float AgentConfidence,
    DateTimeOffset QueuedAt);

public record ReviewDecidedEvent(
    string ReviewId,
    string EmailId,
    string Action,
    string ReviewerId,
    int CorrectionsCount,
    DateTimeOffset DecidedAt);

public record WorkflowCompletedEvent(
    string WorkflowId,
    string EmailId,
    string Path,
    string ClassificationType,
    int TotalDurationMs,
    DateTimeOffset CompletedAt);

public record TaxonomyProposalCreatedEvent(
    string ProposalId,
    string SuggestedLabel,
    float Confidence,
    int SampleCount,
    string[] Signals,
    DateTimeOffset CreatedAt);

public record TaxonomyCategoryCreatedEvent(
    string CategoryId,
    string Label,
    string CreatedBy,
    int RetroactiveReclassificationCount,
    DateTimeOffset CreatedAt);

public record DashboardUpdatedEvent(
    string TriggerType,
    DateTimeOffset OccurredAt);
