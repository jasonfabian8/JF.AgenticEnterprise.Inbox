using Microsoft.AspNetCore.SignalR;

namespace JF.AgenticEnterprise.Api.Hubs;

/// <summary>
/// SignalR hub for real-time agent events.
/// Clients join a workflow group to receive events scoped to that workflow.
/// </summary>
public sealed class InboxHub : Hub
{
    public async Task JoinWorkflow(string workflowId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, WorkflowGroup(workflowId));
    }

    public async Task LeaveWorkflow(string workflowId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, WorkflowGroup(workflowId));
    }

    public static string WorkflowGroup(string workflowId) => $"workflow:{workflowId}";
}
