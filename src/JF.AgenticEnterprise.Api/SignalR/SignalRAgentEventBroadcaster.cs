using JF.AgenticEnterprise.Api.Hubs;
using JF.AgenticEnterprise.Application.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace JF.AgenticEnterprise.Api.SignalR;

/// <summary>
/// Implements IAgentEventBroadcaster using SignalR.
/// Lives in the Api project to avoid reversing the dependency direction
/// (Infrastructure must not reference Api).
/// </summary>
public sealed class SignalRAgentEventBroadcaster : IAgentEventBroadcaster
{
    private readonly IHubContext<InboxHub> _hub;

    public SignalRAgentEventBroadcaster(IHubContext<InboxHub> hub) => _hub = hub;

    public Task BroadcastStartedAsync(AgentStartedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.started", new
               {
                   workflowId = evt.WorkflowId,
                   agent      = evt.Agent,
                   emailId    = evt.EmailId,
               }, ct);

    public Task BroadcastCompletedAsync(AgentCompletedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.completed", new
               {
                   workflowId = evt.WorkflowId,
                   agent      = evt.Agent,
                   emailId    = evt.EmailId,
                   category   = evt.Category,
                   confidence = evt.Confidence,
                   reasoning  = evt.Reasoning,
               }, ct);

    public Task BroadcastFailedAsync(AgentFailedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.failed", new
               {
                   workflowId = evt.WorkflowId,
                   agent      = evt.Agent,
                   emailId    = evt.EmailId,
                   error      = evt.Error,
               }, ct);
}
