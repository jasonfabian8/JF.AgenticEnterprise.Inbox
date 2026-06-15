using JF.AgenticEnterprise.Api.Hubs;
using JF.AgenticEnterprise.Application.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace JF.AgenticEnterprise.Api.SignalR;

/// <summary>
/// Implements IAgentEventBroadcaster using SignalR hub groups.
/// Lives in the Api project to avoid reversing the dependency direction
/// (Infrastructure must not reference Api).
/// </summary>
public sealed class SignalRAgentEventBroadcaster : IAgentEventBroadcaster
{
    private readonly IHubContext<InboxHub> _hub;

    public SignalRAgentEventBroadcaster(IHubContext<InboxHub> hub) => _hub = hub;

    // ── Agent-level events ────────────────────────────────────────────────────

    public Task BroadcastStartedAsync(AgentStartedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.started", new
               {
                   workflowId = evt.WorkflowId,
                   agent = evt.Agent,
                   emailId = evt.EmailId,
                   timestamp = evt.Timestamp,
               }, ct);

    public Task BroadcastCompletedAsync(AgentCompletedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.completed", new
               {
                   workflowId = evt.WorkflowId,
                   agent = evt.Agent,
                   emailId = evt.EmailId,
                   category = evt.Category,
                   confidence = evt.Confidence,
                   reasoning = evt.Reasoning,
                   timestamp = evt.Timestamp,
               }, ct);

    public Task BroadcastFailedAsync(AgentFailedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("agent.failed", new
               {
                   workflowId = evt.WorkflowId,
                   agent = evt.Agent,
                   emailId = evt.EmailId,
                   error = evt.Error,
                   timestamp = evt.Timestamp,
               }, ct);

    // ── Workflow-level events ─────────────────────────────────────────────────

    public Task BroadcastWorkflowUpdatedAsync(WorkflowUpdatedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("workflow.updated", new
               {
                   workflowId = evt.WorkflowId,
                   emailId = evt.EmailId,
                   status = evt.Status,
                   currentStep = evt.CurrentStep,
                   nextAgent = evt.NextAgent,
                   timestamp = evt.Timestamp,
               }, ct);

    public Task BroadcastWorkflowCompletedAsync(WorkflowCompletedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("workflow.completed", new
               {
                   workflowId = evt.WorkflowId,
                   emailId = evt.EmailId,
                   finalStatus = evt.FinalStatus,
                   classificationCategory = evt.ClassificationCategory,
                   routedToAgent = evt.RoutedToAgent,
                   invoiceAnalysisId = evt.InvoiceAnalysisId,
                   contractAnalysisId = evt.ContractAnalysisId,
                   timestamp = evt.Timestamp,
               }, ct);

    // ── Sprint 3 — Reasoning / Collaboration events ───────────────────────────

    public Task BroadcastConflictDetectedAsync(ConflictDetectedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("conflict.detected", new
               {
                   workflowId       = evt.WorkflowId,
                   emailId          = evt.EmailId,
                   conflictId       = evt.ConflictId,
                   conflictType     = evt.ConflictType,
                   sourceAgent      = evt.SourceAgent,
                   targetAgent      = evt.TargetAgent,
                   sourceValue      = evt.SourceValue,
                   targetValue      = evt.TargetValue,
                   sourceConfidence = evt.SourceConfidence,
                   targetConfidence = evt.TargetConfidence,
                   description      = evt.Description,
                   timestamp        = evt.Timestamp,
               }, ct);

    public Task BroadcastTaxonomySuggestedAsync(TaxonomySuggestedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("taxonomy.suggested", new
               {
                   workflowId        = evt.WorkflowId,
                   emailId           = evt.EmailId,
                   proposalId        = evt.ProposalId,
                   suggestedCategory = evt.SuggestedCategory,
                   confidence        = evt.Confidence,
                   reasoning         = evt.Reasoning,
                   timestamp         = evt.Timestamp,
               }, ct);

    public Task BroadcastReviewRequestedAsync(ReviewRequestedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("review.requested", new
               {
                   workflowId     = evt.WorkflowId,
                   emailId        = evt.EmailId,
                   reviewId       = evt.ReviewId,
                   reviewType     = evt.ReviewType,
                   priority       = evt.Priority,
                   question       = evt.Question,
                   recommendation = evt.Recommendation,
                   timestamp      = evt.Timestamp,
               }, ct);

    public Task BroadcastReviewCompletedAsync(ReviewCompletedEvent evt, CancellationToken ct = default)
        => _hub.Clients
               .Group(InboxHub.WorkflowGroup(evt.WorkflowId))
               .SendAsync("review.completed", new
               {
                   workflowId       = evt.WorkflowId,
                   emailId          = evt.EmailId,
                   reviewId         = evt.ReviewId,
                   action           = evt.Action,
                   reviewerId       = evt.ReviewerId,
                   overrideCategory = evt.OverrideCategory,
                   timestamp        = evt.Timestamp,
               }, ct);
}
