using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Entities;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        group.MapPost("/backfill", BackfillAwaitingReview)
             .WithName("BackfillAwaitingReview")
             .WithSummary("Dev utility: creates HumanReview records for workflows stuck in AWAITING_REVIEW without one");

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
        IEmailRepository emailRepo,
        IAgentConflictRepository conflictRepo,
        IWorkflowKnowledgeRepository knowledgeRepo,
        IAgentEventBroadcaster broadcaster,
        IWorkflowOrchestrator orchestrator,
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

        // Advance workflow — approve continues the pipeline; reject marks it failed
        var isApproved = request.Action is ReviewAction.Approve or ReviewAction.ApproveWithCorrections;

        if (isApproved)
        {
            // Run the specialized agent (Invoice/Contract) and finalize
            _ = Task.Run(() => orchestrator.ContinueAfterReviewAsync(
                review.WorkflowId,
                request.OverrideCategory,
                CancellationToken.None), CancellationToken.None);
        }
        else
        {
            var workflow = await workflowRepo.GetByIdAsync(review.WorkflowId, ct);
            if (workflow is not null)
            {
                workflow.Status      = WorkflowStatus.Failed;
                workflow.CompletedAt = DateTimeOffset.UtcNow;
                await workflowRepo.SaveAsync(workflow, ct);
            }

            var email = await emailRepo.GetByIdAsync(review.EmailId, ct);
            if (email is not null)
            {
                email.Status      = EmailStatus.Failed;
                email.ProcessedAt = DateTimeOffset.UtcNow;
                await emailRepo.SaveAsync(email, ct);
            }
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

    // ── POST /api/v1/reviews/backfill ─────────────────────────────────────────

    private static async Task<IResult> BackfillAwaitingReview(
        InboxDbContext db,
        IHumanReviewRepository reviewRepo,
        CancellationToken ct)
    {
        var stuck = await db.Workflows
            .Where(w => w.Status == WorkflowStatus.AwaitingReview)
            .ToListAsync(ct);

        int created = 0;
        foreach (var workflow in stuck)
        {
            var existing = await reviewRepo.GetByWorkflowIdAsync(workflow.Id, ct);
            if (existing.Count > 0) continue;

            var review = new HumanReview
            {
                Id              = Domain.Common.UlidGenerator.NewUlid(),
                EmailId         = workflow.EmailId,
                WorkflowId      = workflow.Id,
                ReviewType      = "CLASSIFICATION_REVIEW",
                Priority        = ReviewPriority.Normal,
                Status          = ReviewStatus.Pending,
                Reason          = "Backfilled: workflow was in AWAITING_REVIEW without a review task.",
                AgentConfidence = 0f,
                QueuedAt        = workflow.StartedAt,
                CreatedAt       = DateTimeOffset.UtcNow,
            };
            await reviewRepo.SaveAsync(review, ct);
            created++;
        }

        return Results.Ok(new { backfilled = created, total = stuck.Count });
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
