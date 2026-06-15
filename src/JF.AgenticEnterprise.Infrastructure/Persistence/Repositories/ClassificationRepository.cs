using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class ClassificationRepository : IClassificationRepository
{
    private readonly InboxDbContext _context;

    public ClassificationRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(Classification classification, CancellationToken ct = default)
    {
        var exists = await _context.Classifications.AnyAsync(c => c.Id == classification.Id, ct);
        if (exists)
            _context.Classifications.Update(classification);
        else
            _context.Classifications.Add(classification);

        await _context.SaveChangesAsync(ct);
        return classification.Id;
    }

    public async Task<Classification?> GetByEmailIdAsync(string emailId, CancellationToken ct = default)
    {
        return await _context.Classifications
            .FirstOrDefaultAsync(c => c.EmailId == emailId, ct);
    }
}
