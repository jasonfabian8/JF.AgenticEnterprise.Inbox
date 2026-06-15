using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class TaxonomyRepository : ITaxonomyRepository
{
    private readonly InboxDbContext _context;

    public TaxonomyRepository(InboxDbContext context) => _context = context;

    public async Task<List<TaxonomyCategory>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.TaxonomyCategories
            .Where(c => c.Status == "ACTIVE")
            .OrderBy(c => c.Label)
            .ToListAsync(ct);
    }

    public async Task<TaxonomyCategory?> GetByLabelAsync(string label, CancellationToken ct = default)
    {
        return await _context.TaxonomyCategories
            .FirstOrDefaultAsync(c => c.Label == label, ct);
    }

    public async Task<string> SaveCategoryAsync(TaxonomyCategory category, CancellationToken ct = default)
    {
        var exists = await _context.TaxonomyCategories.AnyAsync(c => c.Id == category.Id, ct);
        if (exists)
            _context.TaxonomyCategories.Update(category);
        else
            _context.TaxonomyCategories.Add(category);

        await _context.SaveChangesAsync(ct);
        return category.Id;
    }
}
