using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IClassificationRepository
{
    Task<string> SaveAsync(Classification classification, CancellationToken ct = default);
    Task<Classification?> GetByEmailIdAsync(string emailId, CancellationToken ct = default);
}
