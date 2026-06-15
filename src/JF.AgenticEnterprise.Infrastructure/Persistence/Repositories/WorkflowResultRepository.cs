using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class WorkflowResultRepository : IWorkflowResultRepository
{
    private readonly InboxDbContext _context;

    public WorkflowResultRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(WorkflowResult result, CancellationToken ct = default)
    {
        var exists = await _context.WorkflowResults.AnyAsync(r => r.Id == result.Id, ct);
        if (exists) _context.WorkflowResults.Update(result);
        else _context.WorkflowResults.Add(result);

        await _context.SaveChangesAsync(ct);
        return result.Id;
    }

    public async Task<WorkflowResult?> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.WorkflowResults
            .Include(r => r.InvoiceAnalysis)
            .Include(r => r.ContractAnalysis)
            .FirstOrDefaultAsync(r => r.WorkflowId == workflowId, ct);
}
