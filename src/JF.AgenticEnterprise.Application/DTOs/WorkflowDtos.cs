namespace JF.AgenticEnterprise.Application.DTOs;

public record WorkflowDetailDto(
    string WorkflowId,
    string EmailId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OutcomeType,
    List<WorkflowStepDto> Steps,
    List<AgentExecutionDto> AgentExecutions);

public record WorkflowStepDto(
    string Id,
    int StepOrder,
    string StepName,
    string? AgentType,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int DurationMs,
    string? InputSummary,
    string? OutputSummary);

public record AgentExecutionDto(
    string Id,
    string AgentType,
    string AgentVersion,
    string Status,
    float ConfidenceScore,
    string ReasoningText,
    int DurationMs,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
