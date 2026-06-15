using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IHumanReviewRepository
{
    Task<string> SaveAsync(HumanReview review, CancellationToken ct = default);
    Task<HumanReview?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<HumanReview>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);

    /// <summary>All reviews in PENDING or OPEN status, ordered by priority then QueuedAt.</summary>
    Task<IReadOnlyList<HumanReview>> GetPendingAsync(CancellationToken ct = default);

    Task UpdateAsync(HumanReview review, CancellationToken ct = default);
}
