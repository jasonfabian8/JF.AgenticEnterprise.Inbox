using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class HumanReviewEndpoints
{
    public static IEndpointRouteBuilder MapHumanReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reviews").WithTags("Human Reviews");

        group.MapGet("/", GetPendingReviews)
             .WithName("GetPendingReviews")
             .WithSummary("Get the pending human review queue, ordered by priority then age");

        group.MapGet("/{id}", GetReviewById)
             .WithName("GetReviewById")
             .WithSummary("Get a single human review by id");

        group.MapPost("/{id}/decide", DecideReview)
             .WithName("DecideReview")
             .WithSummary("Submit a human decision (approve / reject / override) for a review");

        return app;
    }

    // ── GET /api/v1/reviews ───────────────────────────────────────────────────

    private static async Task<IResult> GetPendingReviews(
        IHumanReviewRepository reviewRepo,
        CancellationToken ct)
    {
        var reviews = await reviewRepo.GetPendingAsync(ct);

        var dtos = reviews.Select(MapDto).ToList();

        return Results.Ok(new ReviewQueueDto(
            TotalPending: dtos.Count,
            UrgentCount:  dtos.Count(d => d.Priority == ReviewPriority.Urgent),
            Reviews:      dtos));
    }

    // ── GET /api/v1/reviews/{id} ──────────────────────────────────────────────

    private static async Task<IResult> GetReviewById(
        string id,
        IHumanReviewRepository reviewRepo,
        CancellationToken ct)
    {
        var review = await reviewRepo.GetByIdAsync(id, ct);
        return review is null ? Results.NotFound() : Results.Ok(MapDto(review));
    }

    // ── POST /api/v1/reviews/{id}/decide ─────────────────────────────────────

    private static async Task<IResult> DecideReview(
        string id,
        [FromBody] HumanReviewDecisionRequest request,
        IHumanReviewRepository reviewRepo,
        IWorkflowRepository workflowRepo,
        IAgentConflictRepository conflictRepo,
        IWorkflowKnowledgeRepository knowledgeRepo,
        IAgentEventBroadcaster broadcaster,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
            return Results.BadRequest(new { error = "Action is required." });

        if (string.IsNullOrWhiteSpace(request.ReviewerId))
            return Results.BadRequest(new { error = "ReviewerId is required." });

        var review = await reviewRepo.GetByIdAsync(id, ct);
        if (review is null) return Results.NotFound();

        if (review.Status == ReviewStatus.Decided)
            return Results.Conflict(new { error = "Review has already been decided." });

        // Apply decision
        review.Status              = ReviewStatus.Decided;
        review.Action              = request.Action;
        review.ReviewerId          = request.ReviewerId;
        review.ReviewerNote        = request.ReviewerNote;
        review.OverrideCategory    = request.OverrideCategory;
        review.DecidedAt           = DateTimeOffset.UtcNow;
        review.ReviewDurationSeconds = review.OpenedAt.HasValue
            ? (int)(DateTimeOffset.UtcNow - review.OpenedAt.Value).TotalSeconds
            : 0;
        await reviewRepo.UpdateAsync(review, ct);

        // Resolve the linked conflict if present
        if (review.ConflictId is not null)
        {
            var conflict = await conflictRepo.GetByIdAsync(review.ConflictId, ct);
            if (conflict is not null)
            {
                conflict.Resolution = $"Human decision: {request.Action}" +
                    (request.OverrideCategory is not null
                        ? $" → override category: {request.OverrideCategory}"
                        : string.Empty);
                conflict.ResolvedAt = DateTimeOffset.UtcNow;
                await conflictRepo.UpdateAsync(conflict, ct);
            }
        }

        // Update WorkflowKnowledge with human-approved category
        if (request.OverrideCategory is not null)
        {
            var knowledge = await knowledgeRepo.GetByWorkflowIdAsync(review.WorkflowId, ct);
            if (knowledge is not null)
            {
                knowledge.ApprovedCategory = request.OverrideCategory;
                knowledge.ApprovedBy       = request.ReviewerId;
                knowledge.ApprovedAt       = DateTimeOffset.UtcNow;
                knowledge.CurrentCategory  = request.OverrideCategory;
                knowledge.UpdatedAt        = DateTimeOffset.UtcNow;
                await knowledgeRepo.UpdateAsync(knowledge, ct);
            }
        }

        // Advance workflow status
        var workflow = await workflowRepo.GetByIdAsync(review.WorkflowId, ct);
        if (workflow is not null)
        {
            workflow.Status = request.Action == ReviewAction.Approve ||
                              request.Action == ReviewAction.ApproveWithCorrections
                ? WorkflowStatus.CompletedHuman
                : WorkflowStatus.Failed;
            await workflowRepo.SaveAsync(workflow, ct);
        }

        // Broadcast review.completed
        await broadcaster.BroadcastReviewCompletedAsync(new ReviewCompletedEvent(
            review.WorkflowId,
            review.EmailId,
            review.Id,
            request.Action,
            request.ReviewerId,
            request.OverrideCategory,
            DateTimeOffset.UtcNow), ct);

        return Results.Ok(MapDto(review));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static HumanReviewDto MapDto(HumanReview r) => new(
        r.Id, r.EmailId, r.WorkflowId,
        r.ReviewType, r.Priority, r.Status,
        r.Reason, r.AgentConfidence,
        r.ConflictId, r.AssignedTo,
        Question:       null,   // not persisted on entity — surfaced via SignalR at request time
        Recommendation: null,
        r.Action, r.OverrideCategory,
        r.ReviewerNote, r.ReviewerId,
        r.QueuedAt, r.OpenedAt, r.DecidedAt);
}
