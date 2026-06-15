using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class ContractAnalysisRepository : IContractAnalysisRepository
{
    private readonly InboxDbContext _context;

    public ContractAnalysisRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(ContractAnalysis analysis, CancellationToken ct = default)
    {
        var exists = await _context.ContractAnalyses.AnyAsync(a => a.Id == analysis.Id, ct);
        if (exists) _context.ContractAnalyses.Update(analysis);
        else _context.ContractAnalyses.Add(analysis);

        await _context.SaveChangesAsync(ct);
        return analysis.Id;
    }

    public async Task<ContractAnalysis?> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.ContractAnalyses
            .FirstOrDefaultAsync(a => a.WorkflowId == workflowId, ct);

    public async Task<ContractAnalysis?> GetByEmailIdAsync(
        string emailId, CancellationToken ct = default)
        => await _context.ContractAnalyses
            .FirstOrDefaultAsync(a => a.EmailId == emailId, ct);
}
