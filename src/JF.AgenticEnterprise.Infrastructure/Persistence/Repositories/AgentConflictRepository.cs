using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class AgentConflictRepository : IAgentConflictRepository
{
    private readonly InboxDbContext _context;

    public AgentConflictRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(AgentConflict conflict, CancellationToken ct = default)
    {
        var exists = await _context.AgentConflicts.AnyAsync(c => c.Id == conflict.Id, ct);
        if (exists) _context.AgentConflicts.Update(conflict);
        else _context.AgentConflicts.Add(conflict);

        await _context.SaveChangesAsync(ct);
        return conflict.Id;
    }

    public async Task<AgentConflict?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _context.AgentConflicts
            .Include(c => c.HumanReviews)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<AgentConflict>> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.AgentConflicts
            .Where(c => c.WorkflowId == workflowId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task UpdateAsync(AgentConflict conflict, CancellationToken ct = default)
    {
        _context.AgentConflicts.Update(conflict);
        await _context.SaveChangesAsync(ct);
    }
}
