namespace JF.AgenticEnterprise.Application.DTOs;

// ── Workflow detail (GET /emails/{id}/workflow) ───────────────────────────────

public record WorkflowDetailDto(
    string WorkflowId,
    string EmailId,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? OutcomeType,
    List<WorkflowStepDto> Steps,
    List<AgentExecutionDto> AgentExecutions,
    OrchestrationDecisionDto? OrchestrationDecision,
    WorkflowResultDto? WorkflowResult);

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

// ── Orchestration decision ────────────────────────────────────────────────────

public record OrchestrationDecisionDto(
    string ClassificationCategory,
    string NextAgent,
    string WorkflowStatus,
    string Reasoning,
    DateTimeOffset DecidedAt);

// ── Workflow result aggregate ─────────────────────────────────────────────────

public record WorkflowResultDto(
    string FinalStatus,
    string ClassificationCategory,
    float ClassificationConfidence,
    string RoutedToAgent,
    string Summary,
    DateTimeOffset CompletedAt,
    InvoiceAnalysisDto? InvoiceAnalysis,
    ContractAnalysisDto? ContractAnalysis);

// ── Specialized analysis DTOs ─────────────────────────────────────────────────

public record InvoiceAnalysisDto(
    string Id,
    string? Supplier,
    string? InvoiceNumber,
    string? InvoiceDate,
    string? DueDate,
    string? Currency,
    decimal? TotalAmount,
    float Confidence,
    string Summary,
    DateTimeOffset CreatedAt);

public record ContractAnalysisDto(
    string Id,
    string? ContractType,
    List<string> Parties,
    string? EffectiveDate,
    string? ExpirationDate,
    string? RenewalClause,
    List<string> KeyObligations,
    float Confidence,
    string Reasoning,
    DateTimeOffset CreatedAt);

// ── Workflow status (GET /workflows/{id}/status) ──────────────────────────────

public record WorkflowStatusDto(
    string WorkflowId,
    string EmailId,
    string Status,
    string? CurrentStep,
    string? OutcomeType,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

// ── Execute (POST /workflows/{id}/execute) ────────────────────────────────────

public record WorkflowExecuteResponse(
    string WorkflowId,
    string Status,
    string Message);

// ── Agent execution list (GET /workflows/{id}/executions) ─────────────────────

public record AgentExecutionListResponse(
    string WorkflowId,
    List<AgentExecutionDto> Executions);
