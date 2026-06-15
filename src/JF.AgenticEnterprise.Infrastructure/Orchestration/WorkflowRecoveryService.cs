using JF.AgenticEnterprise.Application.Orchestration;
using JF.AgenticEnterprise.Domain.Entities;
using JF.AgenticEnterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JF.AgenticEnterprise.Infrastructure.Orchestration;

/// <summary>
/// On startup, resumes any workflow left in Processing state (e.g., due to a crash).
/// Workflows in AwaitingReview are intentionally left untouched — they need human input.
/// </summary>
public sealed class WorkflowRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowRecoveryService> _logger;

    public WorkflowRecoveryService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowRecoveryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Brief delay so the rest of the app (SignalR hub, etc.) is fully started.
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db         = scope.ServiceProvider.GetRequiredService<InboxDbContext>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();

        // Find workflows that were actively processing when the service stopped.
        // AwaitingReview and AwaitingTaxonomyApproval are intentionally skipped —
        // they are waiting on external input (human or taxonomy approval).
        var stuck = await db.Workflows
            .Where(w => w.Status == WorkflowStatus.Processing ||
                        w.Status == WorkflowStatus.Escalated)
            .Select(w => w.Id)
            .ToListAsync(stoppingToken);

        if (stuck.Count == 0)
        {
            _logger.LogInformation("WorkflowRecovery: no in-progress workflows found.");
            return;
        }

        _logger.LogWarning(
            "WorkflowRecovery: found {Count} in-progress workflow(s) — resuming.", stuck.Count);

        foreach (var workflowId in stuck)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Each workflow gets its own scope so a failure doesn't affect others.
            await using var wfScope = _scopeFactory.CreateAsyncScope();
            var orch = wfScope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();
            try
            {
                _logger.LogInformation("WorkflowRecovery: resuming workflow {WorkflowId}", workflowId);
                await orch.ExecuteAsync(workflowId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "WorkflowRecovery: workflow {WorkflowId} failed during recovery", workflowId);
            }
        }
    }
}
