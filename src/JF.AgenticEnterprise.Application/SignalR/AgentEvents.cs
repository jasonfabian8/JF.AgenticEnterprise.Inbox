namespace JF.AgenticEnterprise.Application.SignalR;

public sealed record AgentStartedEvent(
    string WorkflowId,
    string Agent,
    string EmailId);

public sealed record AgentCompletedEvent(
    string WorkflowId,
    string Agent,
    string EmailId,
    string Category,
    float Confidence,
    string Reasoning);

public sealed record AgentFailedEvent(
    string WorkflowId,
    string Agent,
    string EmailId,
    string Error);
