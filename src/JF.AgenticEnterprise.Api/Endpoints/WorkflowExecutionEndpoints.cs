using JF.AgenticEnterprise.Application.DTOs;
using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Application.Repositories;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class WorkflowExecutionEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowExecutionEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/workflows").WithTags("Workflow Execution");

        group.MapPost("/{workflowId}/execute", ExecuteWorkflow)
             .WithName("ExecuteWorkflow")
             .WithSummary("Trigger agent execution for an existing workflow");

        group.MapGet("/{workflowId}/executions", GetExecutions)
             .WithName("GetWorkflowExecutions")
             .WithSummary("List all agent executions for a workflow");

        group.MapGet("/{workflowId}/status", GetStatus)
             .WithName("GetWorkflowStatus")
             .WithSummary("Get current workflow status");

        return app;
    }

    // ── POST /api/v1/workflows/{workflowId}/execute ───────────────────────────

    private static async Task<IResult> ExecuteWorkflow(
        string workflowId,
        IWorkflowOrchestrator orchestrator,
        IWorkflowRepository workflowRepo,
        IServiceScopeFactory scopeFactory,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Results.NotFound();

        // Fire-and-forget with a new DI scope so EF contexts don't conflict
        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var orch = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();
            await orch.ExecuteAsync(workflowId);
        });

        return Results.Accepted($"/api/v1/workflows/{workflowId}/status",
            new WorkflowExecuteResponse(workflowId, "PROCESSING", "Execution started."));
    }

    // ── GET /api/v1/workflows/{workflowId}/executions ─────────────────────────

    private static async Task<IResult> GetExecutions(
        string workflowId,
        IWorkflowRepository workflowRepo,
        IAgentExecutionRepository executionRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Results.NotFound();

        var executions = await executionRepo.GetByWorkflowIdAsync(workflowId, ct);

        var dtos = executions.Select(a => new AgentExecutionDto(
            a.Id, a.AgentType, a.AgentVersion, a.Status,
            a.ConfidenceScore, a.ReasoningText,
            a.DurationMs, a.StartedAt, a.CompletedAt,
            a.ErrorMessage, a.OutputPayloadJson)).ToList();

        return Results.Ok(new AgentExecutionListResponse(workflowId, dtos));
    }

    // ── GET /api/v1/workflows/{workflowId}/status ─────────────────────────────

    private static async Task<IResult> GetStatus(
        string workflowId,
        IWorkflowRepository workflowRepo,
        CancellationToken ct)
    {
        var workflow = await workflowRepo.GetByIdAsync(workflowId, ct);
        if (workflow is null) return Results.NotFound();

        return Results.Ok(new WorkflowStatusDto(
            WorkflowId: workflow.Id,
            EmailId: workflow.EmailId,
            Status: workflow.Status,
            CurrentStep: workflow.CurrentStep,
            OutcomeType: workflow.OutcomeType,
            StartedAt: workflow.StartedAt,
            CompletedAt: workflow.CompletedAt));
    }
}
