namespace JF.AgenticEnterprise.Application.SignalR;

public interface IAgentEventBroadcaster
{
    // ── Agent-level ───────────────────────────────────────────────────────────
    Task BroadcastStartedAsync  (AgentStartedEvent   evt, CancellationToken ct = default);
    Task BroadcastCompletedAsync(AgentCompletedEvent evt, CancellationToken ct = default);
    Task BroadcastFailedAsync   (AgentFailedEvent    evt, CancellationToken ct = default);

    // ── Workflow-level ────────────────────────────────────────────────────────
    Task BroadcastWorkflowUpdatedAsync  (WorkflowUpdatedEvent   evt, CancellationToken ct = default);
    Task BroadcastWorkflowCompletedAsync(WorkflowCompletedEvent evt, CancellationToken ct = default);
}
