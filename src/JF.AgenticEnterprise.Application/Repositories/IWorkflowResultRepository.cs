using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IWorkflowResultRepository
{
    Task<string> SaveAsync(WorkflowResult result, CancellationToken ct = default);
    Task<WorkflowResult?> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
}
