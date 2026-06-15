using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class HumanReviewRepository : IHumanReviewRepository
{
    private readonly InboxDbContext _context;

    public HumanReviewRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(HumanReview review, CancellationToken ct = default)
    {
        var exists = await _context.HumanReviews.AnyAsync(r => r.Id == review.Id, ct);
        if (exists) _context.HumanReviews.Update(review);
        else _context.HumanReviews.Add(review);

        await _context.SaveChangesAsync(ct);
        return review.Id;
    }

    public async Task<HumanReview?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.HumanReviews
            .Include(r => r.Conflict)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<HumanReview>> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.HumanReviews
            .Where(r => r.WorkflowId == workflowId)
            .OrderBy(r => r.QueuedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HumanReview>> GetPendingAsync(CancellationToken ct = default)
        => await _context.HumanReviews
            .Where(r => r.Status == ReviewStatus.Pending || r.Status == ReviewStatus.Open)
            .OrderBy(r => r.Priority == ReviewPriority.Urgent ? 0 :
                          r.Priority == ReviewPriority.Normal ? 1 : 2)
            .ThenBy(r => r.QueuedAt)
            .ToListAsync(ct);

    public async Task UpdateAsync(HumanReview review, CancellationToken ct = default)
    {
        _context.HumanReviews.Update(review);
        await _context.SaveChangesAsync(ct);
    }
}
