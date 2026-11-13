// SQLite-backed IScanStatistics: every number is computed by the database from the retained history rows.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tollgate.Application;
using Tollgate.Domain;

namespace Tollgate.Infrastructure.Persistence;

public sealed class SqliteScanStatistics(
    IDbContextFactory<TollgateDbContext> contexts,
    IOptions<HistoryOptions> options,
    TimeProvider clock) : IScanStatistics
{
    private readonly HistoryOptions settings = options.Value;

    public async Task<ScanStatistics> GetAsync(CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var rows = db.ScanHistory.AsNoTracking().Retained(clock.GetUtcNow(), settings.RetentionDays);

        var total = await rows.CountAsync(cancellationToken);
        if (total == 0)
        {
            return new ScanStatistics(0, null, new VerdictCounts(0, 0, 0), []);
        }

        var average = await rows.AverageAsync(r => (double)r.Score, cancellationToken);

        var perVerdict = await rows
            .GroupBy(r => r.Verdict)
            .Select(g => new { Verdict = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Verdict, g => g.Count, cancellationToken);

        // Projected into an anonymous type first: EF cannot sort on a member of a record built by a constructor call.
        var busiestHosts = await rows
            .GroupBy(r => r.Host)
            .Select(g => new { Host = g.Key, Scans = g.Count(), AverageScore = g.Average(r => (double)r.Score) })
            .OrderByDescending(h => h.Scans)
            .ThenBy(h => h.Host)
            .Take(settings.TopHostsCount)
            .ToListAsync(cancellationToken);

        return new ScanStatistics(
            total,
            average,
            new VerdictCounts(
                perVerdict.GetValueOrDefault(Verdict.Dangerous),
                perVerdict.GetValueOrDefault(Verdict.Suspicious),
                perVerdict.GetValueOrDefault(Verdict.Safe)),
            busiestHosts.Select(h => new HostStatistics(h.Host, h.Scans, h.AverageScore)).ToList());
    }
}
