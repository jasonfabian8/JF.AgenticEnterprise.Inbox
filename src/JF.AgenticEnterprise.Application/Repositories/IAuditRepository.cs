using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Application.Repositories;

public interface IAuditRepository
{
    Task AppendAsync(AuditEntry entry, CancellationToken ct = default);
}
