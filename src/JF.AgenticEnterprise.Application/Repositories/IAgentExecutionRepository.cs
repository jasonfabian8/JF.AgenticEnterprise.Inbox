using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IAgentExecutionRepository
{
    Task<string> SaveAsync(AgentExecution execution, CancellationToken ct = default);
    Task<List<AgentExecution>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    Task<AgentExecution?> GetByIdAsync(string id, CancellationToken ct = default);
}
