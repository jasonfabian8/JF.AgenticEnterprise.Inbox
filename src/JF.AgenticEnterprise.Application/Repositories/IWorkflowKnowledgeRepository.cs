using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IWorkflowKnowledgeRepository
{
    Task<string> SaveAsync(WorkflowKnowledge knowledge, CancellationToken ct = default);
    Task<WorkflowKnowledge?> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    Task UpdateAsync(WorkflowKnowledge knowledge, CancellationToken ct = default);
}
