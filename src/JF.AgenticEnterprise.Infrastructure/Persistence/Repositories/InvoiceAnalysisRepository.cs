using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class InvoiceAnalysisRepository : IInvoiceAnalysisRepository
{
    private readonly InboxDbContext _context;

    public InvoiceAnalysisRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(InvoiceAnalysis analysis, CancellationToken ct = default)
    {
        var exists = await _context.InvoiceAnalyses.AnyAsync(a => a.Id == analysis.Id, ct);
        if (exists) _context.InvoiceAnalyses.Update(analysis);
        else _context.InvoiceAnalyses.Add(analysis);

        await _context.SaveChangesAsync(ct);
        return analysis.Id;
    }

    public async Task<InvoiceAnalysis?> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.InvoiceAnalyses
            .FirstOrDefaultAsync(a => a.WorkflowId == workflowId, ct);

    public async Task<InvoiceAnalysis?> GetByEmailIdAsync(
        string emailId, CancellationToken ct = default)
        => await _context.InvoiceAnalyses
            .FirstOrDefaultAsync(a => a.EmailId == emailId, ct);
}
