using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly InboxDbContext _context;

    public EmailRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(Email email, CancellationToken ct = default)
    {
        var exists = await _context.Emails.AnyAsync(e => e.Id == email.Id, ct);
        if (exists)
            _context.Emails.Update(email);
        else
            _context.Emails.Add(email);

        await _context.SaveChangesAsync(ct);
        return email.Id;
    }

    public async Task<Email?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _context.Emails
            .Include(e => e.Attachments)
            .Include(e => e.Classification)
            // Sprint 1 attachment-based extractions
            .Include(e => e.InvoiceExtraction)
            .Include(e => e.ContractExtraction!)
                .ThenInclude(c => c.RiskFlags)
            // Sprint 2 agent-based analysis
            .Include(e => e.InvoiceAnalysis)
            .Include(e => e.ContractAnalysis)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<(List<Email> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? status, string? categoryType, CancellationToken ct = default)
    {
        var query = _context.Emails
            .Include(e => e.Classification)
            .Include(e => e.Attachments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrWhiteSpace(categoryType))
            query = query.Where(e => e.Classification != null &&
                                     e.Classification.CategoryType == categoryType);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct = default)
    {
        return await _context.Emails.AnyAsync(e => e.IdempotencyKey == key, ct);
    }
}
