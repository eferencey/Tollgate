// SQLite-backed IEvidenceCache: evidence per URL, valid for ScanOptions.CacheDuration counted from the
// first scan. Because it lives in the database file it survives a restart.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tollgate.Application;

namespace Tollgate.Infrastructure.Persistence;

public sealed class SqliteEvidenceCache(
    IDbContextFactory<TollgateDbContext> contexts,
    IOptions<ScanOptions> options,
    TimeProvider clock) : IEvidenceCache
{
    private readonly TimeSpan lifetime = options.Value.CacheDuration;

    public async Task<CachedScan?> GetAsync(string normalizedUrl, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var row = await db.CachedEvidence.AsNoTracking().SingleOrDefaultAsync(r => r.Url == normalizedUrl, cancellationToken);

        if (row is null || clock.GetUtcNow() >= row.ScannedAt + lifetime)
        {
            return null;
        }

        return EvidenceJson.TryRead(row.EvidenceJson) is { } evidence ? new CachedScan(evidence, row.ScannedAt) : null;
    }

    public async Task SetAsync(string normalizedUrl, CachedScan scan, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var json = EvidenceJson.Write(scan.Evidence);

        var row = await db.CachedEvidence.SingleOrDefaultAsync(r => r.Url == normalizedUrl, cancellationToken);
        if (row is null)
        {
            db.CachedEvidence.Add(new CachedEvidenceRow { Url = normalizedUrl, EvidenceJson = json, ScannedAt = scan.ScannedAt });
        }
        else
        {
            row.EvidenceJson = json;
            row.ScannedAt = scan.ScannedAt;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Without this, URLs that are never asked for again would stay in the file forever.
        var expiredBefore = clock.GetUtcNow() - lifetime;
        await db.CachedEvidence.Where(r => r.ScannedAt <= expiredBefore).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RemoveAsync(string normalizedUrl, CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.CachedEvidence.Where(r => r.Url == normalizedUrl).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await db.CachedEvidence.ExecuteDeleteAsync(cancellationToken);
    }
}
