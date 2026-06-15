namespace JF.AgenticEnterprise.Application.Agents;

public interface IOrchestratorAgent
{
    Task<OrchestratorResult> DecideAsync(
        OrchestratorRequest request,
        CancellationToken ct = default);
}

public sealed record OrchestratorRequest(
    string WorkflowId,
    string EmailId,
    string ClassificationCategory,
    float ClassificationConfidence,
    string ClassificationReasoning);

public sealed record OrchestratorResult(
    /// <summary>
    /// One of: "InvoiceAgent" | "ContractAgent" | "HumanReview" | "Complete"
    /// </summary>
    string NextAgent,

    /// <summary>Suggested workflow status after routing.</summary>
    string WorkflowStatus,

    string Reasoning);
