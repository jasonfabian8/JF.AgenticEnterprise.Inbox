using JF.AgenticEnterprise.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JF.AgenticEnterprise.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/dashboard/stats", GetStats)
           .WithTags("Dashboard")
           .WithName("GetDashboardStats")
           .WithSummary("Aggregate metrics for the dashboard");

        return app;
    }

    private static async Task<IResult> GetStats(InboxDbContext db, CancellationToken ct)
    {
        // ── Email counts ──────────────────────────────────────────────────────
        var emailStatuses = await db.Emails
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var totalEmails      = emailStatuses.Sum(x => x.Count);
        var completedAuto    = emailStatuses.FirstOrDefault(x => x.Status == "COMPLETED_AUTO")?.Count   ?? 0;
        var completedHuman   = emailStatuses.FirstOrDefault(x => x.Status == "COMPLETED_HUMAN")?.Count  ?? 0;
        var awaitingReview   = emailStatuses.FirstOrDefault(x => x.Status == "AWAITING_REVIEW")?.Count  ?? 0;
        var failedCount      = emailStatuses.FirstOrDefault(x => x.Status == "FAILED")?.Count           ?? 0;
        var processingCount  = emailStatuses.FirstOrDefault(x => x.Status == "PROCESSING")?.Count       ?? 0;

        // ── Category distribution (from classifications) ───────────────────
        var categoryDist = await db.Classifications
            .GroupBy(c => c.CategoryType)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        // ── Confidence band breakdown ──────────────────────────────────────
        var allConf = await db.Classifications
            .Select(c => (double)c.Confidence)
            .ToListAsync(ct);

        var highConf   = allConf.Count(c => c >= 0.85);
        var mediumConf = allConf.Count(c => c >= 0.70 && c < 0.85);
        var lowConf    = allConf.Count(c => c < 0.70);
        var avgConf    = allConf.Count > 0 ? allConf.Average() : 0.0;

        // ── Agent execution stats ──────────────────────────────────────────
        var agentStats = await db.AgentExecutions
            .GroupBy(a => a.AgentType)
            .Select(g => new
            {
                AgentType     = g.Key,
                TotalRuns     = g.Count(),
                Completed     = g.Count(a => a.Status == "COMPLETED"),
                Failed        = g.Count(a => a.Status == "FAILED"),
                AvgDurationMs = g.Average(a => (double?)a.DurationMs) ?? 0,
            })
            .ToListAsync(ct);

        // ── Sprint 3 metrics ───────────────────────────────────────────────
        var totalConflicts  = await db.AgentConflicts.CountAsync(ct);
        var activeConflicts = await db.AgentConflicts.CountAsync(c => c.ResolvedAt == null, ct);

        var conflictsByType = await db.AgentConflicts
            .GroupBy(c => c.ConflictType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var pendingReviews   = await db.HumanReviews.CountAsync(r => r.Status == "PENDING", ct);
        var pendingTaxonomy  = await db.TaxonomyProposals.CountAsync(p => p.Status == "PENDING", ct);

        // ── Recent processing throughput (last 7 days by day) ─────────────
        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeMilliseconds();
        var dailyActivity = await db.Emails
            .Where(e => e.IngestedAt != default)
            .Select(e => new { e.IngestedAt, e.Status })
            .ToListAsync(ct);

        var byDay = dailyActivity
            .GroupBy(e => e.IngestedAt.Date)
            .Select(g => new
            {
                Date      = g.Key.ToString("yyyy-MM-dd"),
                Ingested  = g.Count(),
                Completed = g.Count(e => e.Status.StartsWith("COMPLETED")),
            })
            .OrderBy(x => x.Date)
            .TakeLast(7)
            .ToList();

        return Results.Ok(new
        {
            emails = new
            {
                total         = totalEmails,
                completedAuto,
                completedHuman,
                awaitingReview,
                processing    = processingCount,
                failed        = failedCount,
                automationRate = totalEmails > 0
                    ? Math.Round((double)completedAuto / totalEmails * 100, 1)
                    : 0.0,
            },
            classification = new
            {
                distribution = categoryDist.Select(x => new { x.Category, x.Count }),
                confidence   = new
                {
                    average = Math.Round(avgConf * 100, 1),
                    high    = highConf,
                    medium  = mediumConf,
                    low     = lowConf,
                },
            },
            agents = agentStats.Select(a => new
            {
                agentType     = a.AgentType,
                totalRuns     = a.TotalRuns,
                completed     = a.Completed,
                failed        = a.Failed,
                avgDurationMs = Math.Round(a.AvgDurationMs),
                successRate   = a.TotalRuns > 0
                    ? Math.Round((double)a.Completed / a.TotalRuns * 100, 1)
                    : 0.0,
            }),
            conflicts = new
            {
                total  = totalConflicts,
                active = activeConflicts,
                byType = conflictsByType.Select(x => new { x.Type, x.Count }),
            },
            queues = new
            {
                pendingReviews,
                pendingTaxonomy,
            },
            throughput = byDay,
        });
    }
}
