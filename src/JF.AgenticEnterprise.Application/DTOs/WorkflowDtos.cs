namespace JF.AgenticEnterprise.Application.DTOs;

// ── Workflow detail (existing — returned by GET /emails/{id}/workflow) ────────

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
    string? ErrorMessage,
    string? OutputPayloadJson);

// ── Workflow status (GET /api/v1/workflows/{workflowId}/status) ───────────────

public record WorkflowStatusDto(
    string WorkflowId,
    string EmailId,
    string Status,
    string? CurrentStep,
    string? OutcomeType,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

// ── Execute response (POST /api/v1/workflows/{workflowId}/execute) ────────────

public record WorkflowExecuteResponse(
    string WorkflowId,
    string Status,
    string Message);

// ── Agent execution list (GET /api/v1/workflows/{workflowId}/executions) ──────

public record AgentExecutionListResponse(
    string WorkflowId,
    List<AgentExecutionDto> Executions);
