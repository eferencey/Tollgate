// The retention rule in one place, shared by everything that reads or cleans the history table.
namespace Tollgate.Infrastructure.Persistence;

internal static class ScanHistoryQueries
{
    /// <summary>Entries at or before this instant are past the retention period.</summary>
    public static DateTimeOffset OldestRetained(DateTimeOffset now, int retentionDays) => now.AddDays(-retentionDays);

    public static IQueryable<ScanHistoryRow> Retained(this IQueryable<ScanHistoryRow> rows, DateTimeOffset now, int retentionDays)
    {
        var oldest = OldestRetained(now, retentionDays);
        return rows.Where(r => r.ScannedAt > oldest);
    }
}
