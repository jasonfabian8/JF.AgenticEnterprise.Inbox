using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Application.Services;

namespace JF.AgenticEnterprise.Api.Endpoints;

/// <summary>
/// Sprint 3 reasoning endpoints — conflicts, knowledge, and full timeline
/// for a specific workflow.
/// </summary>
public static class WorkflowReasoningEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowReasoningEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/emails/{emailId}/workflow")
                       .WithTags("Workflow Reasoning");

        group.MapGet("/extended", GetExtendedWorkflow)
             .WithName("GetExtendedWorkflow")
             .WithSummary("Get the full workflow including Sprint 3 conflicts, " +
                          "knowledge state, reviews and taxonomy proposals");

        group.MapGet("/conflicts", GetConflicts)
             .WithName("GetWorkflowConflicts")
             .WithSummary("Get all agent conflicts detected during workflow execution");

        group.MapGet("/knowledge", GetKnowledge)
             .WithName("GetWorkflowKnowledge")
             .WithSummary("Get the evolving document understanding (WorkflowKnowledge)");

        group.MapGet("/timeline", GetReasoningTimeline)
             .WithName("GetReasoningTimeline")
             .WithSummary("Get the full chronological reasoning timeline for this workflow");

        return app;
    }

    // ── GET /api/v1/emails/{emailId}/workflow/extended ────────────────────────

    private static async Task<IResult> GetExtendedWorkflow(
        string emailId,
        IWorkflowRepository workflowRepo,
        IAgentConflictRepository conflictRepo,
        IHumanReviewRepository reviewRepo,
        ITaxonomyProposalRepository proposalRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var od = workflow.OrchestrationDecision;
        var wr = workflow.WorkflowResult;
        var wk = workflow.WorkflowKnowledge;

        // Additional Sprint 3 collections from dedicated repos (authoritative)
        var conflicts = await conflictRepo.GetByWorkflowIdAsync(workflow.Id, ct);
        var reviews   = await reviewRepo.GetByWorkflowIdAsync(workflow.Id, ct);
        var proposals = await proposalRepo.GetByWorkflowIdAsync(workflow.Id, ct);

        var dto = new WorkflowDetailExtendedDto(
            WorkflowId:   workflow.Id,
            EmailId:      workflow.EmailId,
            Status:       workflow.Status,
            StartedAt:    workflow.StartedAt,
            CompletedAt:  workflow.CompletedAt,
            OutcomeType:  workflow.OutcomeType,

            Steps: workflow.Steps.Select(s => new WorkflowStepDto(
                s.Id, s.StepOrder, s.StepName, s.AgentType,
                s.Status, s.StartedAt, s.CompletedAt,
                s.DurationMs, s.InputSummary, s.OutputSummary)).ToList(),

            AgentExecutions: workflow.AgentExecutions.Select(a => new AgentExecutionDto(
                a.Id, a.AgentType, a.AgentVersion, a.Status,
                a.ConfidenceScore, a.ReasoningText,
                a.DurationMs, a.StartedAt, a.CompletedAt,
                a.ErrorMessage, a.OutputPayloadJson)).ToList(),

            OrchestrationDecision: od is null ? null : new OrchestrationDecisionDto(
                od.ClassificationCategory, od.NextAgent,
                od.WorkflowStatus, od.Reasoning, od.DecidedAt),

            WorkflowResult: wr is null ? null : MapWorkflowResultDto(wr),

            // Sprint 3
            Conflicts: conflicts.Select(MapConflictDto).ToList(),

            Knowledge: wk is null ? null : new WorkflowKnowledgeDto(
                wk.Id, wk.WorkflowId,
                wk.InitialCategory, wk.InitialConfidence,
                wk.RefinedCategory, wk.RefinedConfidence, wk.RefinedReasoning,
                wk.SuggestedCategory, wk.SuggestionConfidence, wk.SuggestionReasoning,
                wk.ApprovedCategory, wk.ApprovedBy, wk.ApprovedAt,
                wk.CurrentCategory, wk.CurrentConfidence, wk.CurrentReasoning,
                wk.CreatedAt, wk.UpdatedAt),

            HumanReviews: reviews.Select(MapReviewDto).ToList(),

            TaxonomyProposals: proposals.Select(MapProposalDto).ToList());

        return Results.Ok(dto);
    }

    // ── GET /api/v1/emails/{emailId}/workflow/conflicts ───────────────────────

    private static async Task<IResult> GetConflicts(
        string emailId,
        IWorkflowRepository workflowRepo,
        IAgentConflictRepository conflictRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var conflicts = await conflictRepo.GetByWorkflowIdAsync(workflow.Id, ct);
        return Results.Ok(conflicts.Select(MapConflictDto).ToList());
    }

    // ── GET /api/v1/emails/{emailId}/workflow/knowledge ───────────────────────

    private static async Task<IResult> GetKnowledge(
        string emailId,
        IWorkflowRepository workflowRepo,
        IWorkflowKnowledgeRepository knowledgeRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var wk = await knowledgeRepo.GetByWorkflowIdAsync(workflow.Id, ct);
        if (wk is null) return Results.NotFound();

        return Results.Ok(new WorkflowKnowledgeDto(
            wk.Id, wk.WorkflowId,
            wk.InitialCategory, wk.InitialConfidence,
            wk.RefinedCategory, wk.RefinedConfidence, wk.RefinedReasoning,
            wk.SuggestedCategory, wk.SuggestionConfidence, wk.SuggestionReasoning,
            wk.ApprovedCategory, wk.ApprovedBy, wk.ApprovedAt,
            wk.CurrentCategory, wk.CurrentConfidence, wk.CurrentReasoning,
            wk.CreatedAt, wk.UpdatedAt));
    }

    // ── GET /api/v1/emails/{emailId}/workflow/timeline ────────────────────────

    private static async Task<IResult> GetReasoningTimeline(
        string emailId,
        IWorkflowRepository workflowRepo,
        IReasoningTimelineService timelineService,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var entries = await timelineService.GetTimelineAsync(workflow.Id, ct);

        return Results.Ok(new WorkflowReasoningTimelineDto(
            WorkflowId: workflow.Id,
            Entries: entries.Select(e => new ReasoningTimelineEntryDto(
                e.Timestamp, e.EntryType, e.Actor,
                e.Title, e.Description,
                e.Confidence, e.Status, e.RelatedId)).ToList()));
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static AgentConflictDto MapConflictDto(Domain.Entities.AgentConflict c) => new(
        c.Id, c.WorkflowId, c.EmailId,
        c.SourceAgent, c.TargetAgent,
        c.ConflictType, c.Description,
        c.SourceConfidence, c.TargetConfidence,
        c.SourceValue, c.TargetValue,
        c.Resolution, c.CreatedAt, c.ResolvedAt);

    private static HumanReviewDto MapReviewDto(Domain.Entities.HumanReview r) => new(
        r.Id, r.EmailId, r.WorkflowId,
        r.ReviewType, r.Priority, r.Status,
        r.Reason, r.AgentConfidence,
        r.ConflictId, r.AssignedTo,
        Question: null, Recommendation: null,
        r.Action, r.OverrideCategory,
        r.ReviewerNote, r.ReviewerId,
        r.QueuedAt, r.OpenedAt, r.DecidedAt);

    private static TaxonomyProposalDto MapProposalDto(Domain.Entities.TaxonomyProposal p) => new(
        p.Id, p.SuggestedLabel, p.Status,
        p.Confidence, p.SampleCount,
        p.SuggestedRouting, p.CreatedByAgent,
        p.WorkflowId, p.EmailId,
        p.DecidedBy, p.DecidedAt, p.DecisionNote,
        p.CreatedAt);

    private static WorkflowResultDto MapWorkflowResultDto(Domain.Entities.WorkflowResult wr)
    {
        static List<string> ParseList(string json)
        {
            try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch { return []; }
        }

        return new WorkflowResultDto(
            FinalStatus:              wr.FinalStatus,
            ClassificationCategory:   wr.ClassificationCategory,
            ClassificationConfidence: wr.ClassificationConfidence,
            RoutedToAgent:            wr.RoutedToAgent,
            Summary:                  wr.Summary,
            CompletedAt:              wr.CompletedAt,
            InvoiceAnalysis: wr.InvoiceAnalysis is null ? null : new InvoiceAnalysisDto(
                wr.InvoiceAnalysis.Id,
                wr.InvoiceAnalysis.Supplier,
                wr.InvoiceAnalysis.InvoiceNumber,
                wr.InvoiceAnalysis.InvoiceDate,
                wr.InvoiceAnalysis.DueDate,
                wr.InvoiceAnalysis.Currency,
                wr.InvoiceAnalysis.TotalAmount,
                wr.InvoiceAnalysis.Confidence,
                wr.InvoiceAnalysis.Summary ?? string.Empty,
                wr.InvoiceAnalysis.CreatedAt),
            ContractAnalysis: wr.ContractAnalysis is null ? null : new ContractAnalysisDto(
                wr.ContractAnalysis.Id,
                wr.ContractAnalysis.ContractType,
                ParseList(wr.ContractAnalysis.PartiesJson),
                wr.ContractAnalysis.EffectiveDate,
                wr.ContractAnalysis.ExpirationDate,
                wr.ContractAnalysis.RenewalClause,
                ParseList(wr.ContractAnalysis.KeyObligationsJson),
                wr.ContractAnalysis.Confidence,
                wr.ContractAnalysis.Reasoning,
                wr.ContractAnalysis.CreatedAt));
    }
}
