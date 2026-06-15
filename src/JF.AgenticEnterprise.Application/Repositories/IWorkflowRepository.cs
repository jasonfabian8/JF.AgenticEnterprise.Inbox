using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IWorkflowRepository
{
    Task<string> SaveAsync(Workflow workflow, CancellationToken ct = default);
    Task<Workflow?> GetByEmailIdAsync(string emailId, CancellationToken ct = default);
}
