using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface ITaxonomyProposalRepository
{
    Task<string> SaveAsync(TaxonomyProposal proposal, CancellationToken ct = default);
    Task<TaxonomyProposal?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<TaxonomyProposal>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);

    /// <summary>All proposals in PENDING status, ordered by Confidence descending.</summary>
    Task<IReadOnlyList<TaxonomyProposal>> GetPendingAsync(CancellationToken ct = default);

    Task UpdateAsync(TaxonomyProposal proposal, CancellationToken ct = default);
}
