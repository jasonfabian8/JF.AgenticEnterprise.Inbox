using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IEmailRepository
{
    Task<string> SaveAsync(Email email, CancellationToken ct = default);
    Task<Email?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(List<Email> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? status, string? categoryType, CancellationToken ct = default);
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct = default);
}
