namespace JF.AgenticEnterprise.Application.SignalR;

public interface IAgentEventBroadcaster
{
    Task BroadcastStartedAsync(AgentStartedEvent evt, CancellationToken ct = default);
    Task BroadcastCompletedAsync(AgentCompletedEvent evt, CancellationToken ct = default);
    Task BroadcastFailedAsync(AgentFailedEvent evt, CancellationToken ct = default);
}
