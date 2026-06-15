using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly InboxDbContext _context;

    public WorkflowRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(Workflow workflow, CancellationToken ct = default)
    {
        var exists = await _context.Workflows.AnyAsync(w => w.Id == workflow.Id, ct);
        if (exists) _context.Workflows.Update(workflow);
        else _context.Workflows.Add(workflow);

        await _context.SaveChangesAsync(ct);
        return workflow.Id;
    }

    public async Task<Workflow?> GetByIdAsync(string id, CancellationToken ct = default)
        => await BuildFullQuery()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Workflow?> GetByEmailIdAsync(string emailId, CancellationToken ct = default)
        => await BuildFullQuery()
            .FirstOrDefaultAsync(w => w.EmailId == emailId, ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IQueryable<Workflow> BuildFullQuery()
        => _context.Workflows
            .Include(w => w.Steps.OrderBy(s => s.StepOrder))
            .Include(w => w.AgentExecutions.OrderBy(a => a.StartedAt))
            .Include(w => w.OrchestrationDecision)
            .Include(w => w.WorkflowResult)
                .ThenInclude(r => r!.InvoiceAnalysis)
            .Include(w => w.WorkflowResult)
                .ThenInclude(r => r!.ContractAnalysis);
}
