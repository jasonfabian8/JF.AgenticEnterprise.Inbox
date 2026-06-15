using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IContractAnalysisRepository
{
    Task<string>            SaveAsync(ContractAnalysis analysis, CancellationToken ct = default);
    Task<ContractAnalysis?> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    Task<ContractAnalysis?> GetByEmailIdAsync(string emailId, CancellationToken ct = default);
}
