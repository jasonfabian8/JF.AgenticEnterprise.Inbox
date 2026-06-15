using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.SignalR;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class TaxonomyEndpoints
{
    public static IEndpointRouteBuilder MapTaxonomyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/taxonomy").WithTags("Taxonomy");

        group.MapGet("/proposals", GetPendingProposals)
             .WithName("GetPendingTaxonomyProposals")
             .WithSummary("Get taxonomy proposals awaiting human approval, ordered by confidence");

        group.MapGet("/proposals/{id}", GetProposalById)
             .WithName("GetTaxonomyProposalById")
             .WithSummary("Get a single taxonomy proposal by id");

        group.MapPost("/proposals/{id}/decide", DecideProposal)
             .WithName("DecideTaxonomyProposal")
             .WithSummary("Approve or reject a taxonomy proposal");

        return app;
    }

    // ── GET /api/v1/taxonomy/proposals ───────────────────────────────────────

    private static async Task<IResult> GetPendingProposals(
        ITaxonomyProposalRepository proposalRepo,
        CancellationToken ct)
    {
        var proposals = await proposalRepo.GetPendingAsync(ct);
        var dtos = proposals.Select(MapDto).ToList();

        return Results.Ok(new TaxonomyQueueDto(
            TotalPending: dtos.Count,
            Proposals:    dtos));
    }

    // ── GET /api/v1/taxonomy/proposals/{id} ──────────────────────────────────

    private static async Task<IResult> GetProposalById(
        string id,
        ITaxonomyProposalRepository proposalRepo,
        CancellationToken ct)
    {
        var proposal = await proposalRepo.GetByIdAsync(id, ct);
        return proposal is null ? Results.NotFound() : Results.Ok(MapDto(proposal));
    }

    // ── POST /api/v1/taxonomy/proposals/{id}/decide ───────────────────────────

    private static async Task<IResult> DecideProposal(
        string id,
        [FromBody] TaxonomyProposalDecisionRequest request,
        ITaxonomyProposalRepository proposalRepo,
        ITaxonomyRepository taxonomyRepo,
        IWorkflowKnowledgeRepository knowledgeRepo,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Decision))
            return Results.BadRequest(new { error = "Decision is required (APPROVED or REJECTED)." });

        if (string.IsNullOrWhiteSpace(request.DecidedBy))
            return Results.BadRequest(new { error = "DecidedBy is required." });

        var proposal = await proposalRepo.GetByIdAsync(id, ct);
        if (proposal is null) return Results.NotFound();

        if (proposal.Status != "PENDING")
            return Results.Conflict(new { error = $"Proposal is already {proposal.Status}." });

        proposal.Status      = request.Decision.ToUpperInvariant();
        proposal.DecidedBy   = request.DecidedBy;
        proposal.DecidedAt   = DateTimeOffset.UtcNow;
        proposal.DecisionNote = request.DecisionNote;
        await proposalRepo.UpdateAsync(proposal, ct);

        // When approved, create the new TaxonomyCategory
        if (proposal.Status == "APPROVED")
        {
            var existing = await taxonomyRepo.GetByLabelAsync(proposal.SuggestedLabel, ct);
            if (existing is null)
            {
                await taxonomyRepo.SaveCategoryAsync(new TaxonomyCategory
                {
                    Id        = Domain.Common.UlidGenerator.NewUlid(),
                    Label     = proposal.SuggestedLabel,
                    Status    = "ACTIVE",
                    Routing   = proposal.SuggestedRouting,
                    CreatedAt = DateTimeOffset.UtcNow,
                }, ct);
            }

            // Update WorkflowKnowledge for the triggering workflow if present
            if (proposal.WorkflowId is not null)
            {
                var knowledge = await knowledgeRepo.GetByWorkflowIdAsync(proposal.WorkflowId, ct);
                if (knowledge is not null)
                {
                    knowledge.ApprovedCategory = proposal.SuggestedLabel;
                    knowledge.ApprovedBy       = request.DecidedBy;
                    knowledge.ApprovedAt       = DateTimeOffset.UtcNow;
                    knowledge.CurrentCategory  = proposal.SuggestedLabel;
                    knowledge.UpdatedAt        = DateTimeOffset.UtcNow;
                    await knowledgeRepo.UpdateAsync(knowledge, ct);
                }
            }
        }

        return Results.Ok(MapDto(proposal));
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static TaxonomyProposalDto MapDto(TaxonomyProposal p) => new(
        p.Id, p.SuggestedLabel, p.Status,
        p.Confidence, p.SampleCount,
        p.SuggestedRouting, p.CreatedByAgent,
        p.WorkflowId, p.EmailId,
        p.DecidedBy, p.DecidedAt, p.DecisionNote,
        p.CreatedAt);
}
