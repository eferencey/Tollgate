// Port for the persistent scan history: one entry per finished scan, kept for HistoryOptions.RetentionDays.
using Tollgate.Domain;

namespace Tollgate.Application;

public interface IScanHistory
{
    /// <summary>Appends an entry and drops entries that are older than the retention period.</summary>
    Task AddAsync(ScanHistoryEntry entry, CancellationToken cancellationToken);

    /// <summary>The newest entries first, at most <paramref name="limit"/> of them.</summary>
    Task<IReadOnlyList<ScanHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);

    /// <summary>Deletes one entry and returns its URL, or null when no entry has that id.</summary>
    Task<string?> RemoveAsync(long id, CancellationToken cancellationToken);

    /// <summary>Deletes every entry.</summary>
    Task ClearAsync(CancellationToken cancellationToken);
}

/// <param name="Id">Assigned by the store; 0 for an entry that has not been stored yet.</param>
/// <param name="Url">The normalized URL that was scanned (the full URL, not only the host).</param>
/// <param name="Host">Punycode host of the URL; kept apart so statistics can group by domain.</param>
/// <param name="ScannedAt">When this scan finished (not when the cached evidence was first gathered).</param>
public sealed record ScanHistoryEntry(
    long Id,
    string Url,
    string Host,
    int Score,
    Verdict Verdict,
    Confidence Confidence,
    DateTimeOffset ScannedAt);
