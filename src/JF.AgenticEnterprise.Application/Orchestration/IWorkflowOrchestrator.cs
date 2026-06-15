namespace JF.AgenticEnterprise.Application.Orchestration;

public interface IWorkflowOrchestrator
{
    /// <summary>Creates a workflow for <paramref name="emailId"/> and runs all agents.</summary>
    Task<string> StartForEmailAsync(string emailId, CancellationToken ct = default);

    /// <summary>Re-runs agents for an already-created workflow.</summary>
    Task ExecuteAsync(string workflowId, CancellationToken ct = default);

    /// <summary>
    /// Continues a workflow that was paused for human review.
    /// Runs the appropriate specialized agent based on the classification category,
    /// then finalizes the workflow.
    /// </summary>
    Task ContinueAfterReviewAsync(string workflowId, string? overrideCategory, CancellationToken ct = default);
}
