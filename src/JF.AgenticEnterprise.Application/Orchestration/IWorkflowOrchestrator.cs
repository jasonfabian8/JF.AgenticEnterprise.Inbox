namespace JF.AgenticEnterprise.Application.Orchestration;

public interface IWorkflowOrchestrator
{
    /// <summary>Creates a workflow for <paramref name="emailId"/> and runs all agents.</summary>
    Task<string> StartForEmailAsync(string emailId, CancellationToken ct = default);

    /// <summary>Re-runs agents for an already-created workflow.</summary>
    Task ExecuteAsync(string workflowId, CancellationToken ct = default);
}
