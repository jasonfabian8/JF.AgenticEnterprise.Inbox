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
             .WithSummary("Get the workflow timeline and agent execution chain for an email");

        return app;
    }

    private static async Task<IResult> GetWorkflow(
        string emailId,
        IWorkflowRepository workflowRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByEmailIdAsync(emailId, ct);
        if (workflow is null) return Results.NotFound();

        var dto = new WorkflowDetailDto(
            WorkflowId:  workflow.Id,
            EmailId:     workflow.EmailId,
            Status:      workflow.Status,
            StartedAt:   workflow.StartedAt,
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
                a.ErrorMessage, a.OutputPayloadJson)).ToList());

        return Results.Ok(dto);
    }
}
