using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly InboxDbContext _context;

    public AuditRepository(InboxDbContext context) => _context = context;

    public async Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync(ct);
    }
}
