using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class TaxonomyProposalRepository : ITaxonomyProposalRepository
{
    private readonly InboxDbContext _context;

    public TaxonomyProposalRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(TaxonomyProposal proposal, CancellationToken ct = default)
    {
        var exists = await _context.TaxonomyProposals.AnyAsync(p => p.Id == proposal.Id, ct);
        if (exists) _context.TaxonomyProposals.Update(proposal);
        else _context.TaxonomyProposals.Add(proposal);

        await _context.SaveChangesAsync(ct);
        return proposal.Id;
    }

    public async Task<TaxonomyProposal?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.TaxonomyProposals
            .Include(p => p.Candidates)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<TaxonomyProposal>> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.TaxonomyProposals
            .Where(p => p.WorkflowId == workflowId)
            .OrderByDescending(p => p.Confidence)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TaxonomyProposal>> GetPendingAsync(
        CancellationToken ct = default)
        => await _context.TaxonomyProposals
            .Where(p => p.Status == "PENDING")
            .OrderByDescending(p => p.Confidence)
            .ToListAsync(ct);

    public async Task UpdateAsync(TaxonomyProposal proposal, CancellationToken ct = default)
    {
        _context.TaxonomyProposals.Update(proposal);
        await _context.SaveChangesAsync(ct);
    }
}
