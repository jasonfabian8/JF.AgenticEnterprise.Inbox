using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class OrchestrationDecisionRepository : IOrchestrationDecisionRepository
{
    private readonly InboxDbContext _context;

    public OrchestrationDecisionRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(OrchestrationDecision decision, CancellationToken ct = default)
    {
        var exists = await _context.OrchestrationDecisions.AnyAsync(d => d.Id == decision.Id, ct);
        if (exists) _context.OrchestrationDecisions.Update(decision);
        else _context.OrchestrationDecisions.Add(decision);

        await _context.SaveChangesAsync(ct);
        return decision.Id;
    }

    public async Task<OrchestrationDecision?> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.OrchestrationDecisions
            .FirstOrDefaultAsync(d => d.WorkflowId == workflowId, ct);
}
