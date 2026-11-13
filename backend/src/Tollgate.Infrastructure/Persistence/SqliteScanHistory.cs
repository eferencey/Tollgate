// SQLite-backed IScanHistory. Entries older than HistoryOptions.RetentionDays are invisible to readers
// and physically removed whenever a new entry is written, so no background job is needed.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tollgate.Application;

namespace Tollgate.Infrastructure.Persistence;

public sealed class SqliteScanHistory(
    IDbContextFactory<TollgateDbContext> contexts,
    IOptions<HistoryOptions> options,
    TimeProvider clock) : IScanHistory
{
    private readonly HistoryOptions settings = options.Value;

    public async Task AddAsync(ScanHistoryEntry entry, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);

        db.ScanHistory.Add(new ScanHistoryRow
        {
            Url = entry.Url,
            Host = entry.Host,
            Score = entry.Score,
            Verdict = entry.Verdict,
            Confidence = entry.Confidence,
            ScannedAt = entry.ScannedAt,
        });
        await db.SaveChangesAsync(cancellationToken);

        var oldest = ScanHistoryQueries.OldestRetained(clock.GetUtcNow(), settings.RetentionDays);
        await db.ScanHistory.Where(r => r.ScannedAt <= oldest).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScanHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);

        var rows = await db.ScanHistory.AsNoTracking()
            .Retained(clock.GetUtcNow(), settings.RetentionDays)
            .OrderByDescending(r => r.ScannedAt)
            .ThenByDescending(r => r.Id) // same-instant scans keep their insertion order
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ScanHistoryEntry(r.Id, r.Url, r.Host, r.Score, r.Verdict, r.Confidence, r.ScannedAt))
            .ToList();
    }

    public async Task<string?> RemoveAsync(long id, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);

        var row = await db.ScanHistory.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        db.ScanHistory.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        return row.Url;
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.ScanHistory.ExecuteDeleteAsync(cancellationToken);
    }
}
