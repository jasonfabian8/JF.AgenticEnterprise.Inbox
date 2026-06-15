using JF.AgenticEnterprise.Application.Repositories;
using JF.AgenticEnterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Infrastructure.Persistence.Repositories;

public class AgentExecutionRepository : IAgentExecutionRepository
{
    private readonly InboxDbContext _context;

    public AgentExecutionRepository(InboxDbContext context) => _context = context;

    public async Task<string> SaveAsync(AgentExecution execution, CancellationToken ct = default)
    {
        var exists = await _context.AgentExecutions.AnyAsync(a => a.Id == execution.Id, ct);
        if (exists)
            _context.AgentExecutions.Update(execution);
        else
            _context.AgentExecutions.Add(execution);

        await _context.SaveChangesAsync(ct);
        return execution.Id;
    }

    public async Task<List<AgentExecution>> GetByWorkflowIdAsync(
        string workflowId, CancellationToken ct = default)
    {
        return await _context.AgentExecutions
            .Where(a => a.WorkflowId == workflowId)
            .OrderBy(a => a.StartedAt)
            .ToListAsync(ct);
    }

    public async Task<AgentExecution?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await _context.AgentExecutions.FirstOrDefaultAsync(a => a.Id == id, ct);
    }
}
