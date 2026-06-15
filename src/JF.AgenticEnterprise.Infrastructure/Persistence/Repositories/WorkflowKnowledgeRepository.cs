using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public sealed class WorkflowKnowledgeRepository : IWorkflowKnowledgeRepository
{
    private readonly InboxDbContext _context;

    public WorkflowKnowledgeRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(WorkflowKnowledge knowledge, CancellationToken ct = default)
    {
        var exists = await _context.WorkflowKnowledge.AnyAsync(k => k.Id == knowledge.Id, ct);
        if (exists) _context.WorkflowKnowledge.Update(knowledge);
        else _context.WorkflowKnowledge.Add(knowledge);

        await _context.SaveChangesAsync(ct);
        return knowledge.Id;
    }

    public async Task<WorkflowKnowledge?> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
        => await _context.WorkflowKnowledge
            .FirstOrDefaultAsync(k => k.WorkflowId == workflowId, ct);

    public async Task UpdateAsync(WorkflowKnowledge knowledge, CancellationToken ct = default)
    {
        _context.WorkflowKnowledge.Update(knowledge);
        await _context.SaveChangesAsync(ct);
    }
}
