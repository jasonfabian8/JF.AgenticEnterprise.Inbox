using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IOrchestrationDecisionRepository
{
    Task<string> SaveAsync(OrchestrationDecision decision, CancellationToken ct = default);
    Task<OrchestrationDecision?> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
}
