using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IAgentConflictRepository
{
    Task<string> SaveAsync(AgentConflict conflict, CancellationToken ct = default);
    Task<AgentConflict?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<AgentConflict>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    Task UpdateAsync(AgentConflict conflict, CancellationToken ct = default);
}
