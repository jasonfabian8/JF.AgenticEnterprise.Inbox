using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Repositories;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/emails/{emailId}").WithTags("Workflows");

        group.MapGet("/workflow", GetWorkflow)
             .WithName("GetWorkflow")
             .WithSummary("Get the full workflow timeline, orchestration decision and analysis results");

        return app;
    }

    private static async Task<IResult> GetWorkflow(
        string emailId,
        IWorkflowRepository workflowRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var od = workflow.OrchestrationDecision;
        var wr = workflow.WorkflowResult;

        var dto = new WorkflowDetailDto(
            WorkflowId: workflow.Id,
            EmailId: workflow.EmailId,
            Status: workflow.Status,
            StartedAt: workflow.StartedAt,
            CompletedAt: workflow.CompletedAt,
            OutcomeType: workflow.OutcomeType,

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
                od.ClassificationCategory,
                od.NextAgent,
                od.WorkflowStatus,
                od.Reasoning,
                od.DecidedAt),

            WorkflowResult: wr is null ? null : new WorkflowResultDto(
                FinalStatus: wr.FinalStatus,
                ClassificationCategory: wr.ClassificationCategory,
                ClassificationConfidence: wr.ClassificationConfidence,
                RoutedToAgent: wr.RoutedToAgent,
                Summary: wr.Summary,
                CompletedAt: wr.CompletedAt,
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
                ContractAnalysis: wr.ContractAnalysis is null ? null : MapContractAnalysis(wr.ContractAnalysis)));

        return Results.Ok(dto);
    }

    private static ContractAnalysisDto MapContractAnalysis(
        Domain.Entities.ContractAnalysis ca)
    {
        var parties = TryDeserializeStringList(ca.PartiesJson);
        var obligations = TryDeserializeStringList(ca.KeyObligationsJson);

        return new ContractAnalysisDto(
            ca.Id,
            ca.ContractType,
            parties,
            ca.EffectiveDate,
            ca.ExpirationDate,
            ca.RenewalClause,
            obligations,
            ca.Confidence,
            ca.Reasoning,
            ca.CreatedAt);
    }

    private static List<string> TryDeserializeStringList(string json)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
