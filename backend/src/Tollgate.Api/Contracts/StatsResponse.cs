// The JSON body of GET /api/stats: aggregate numbers over the retained scan history.
using Tollgate.Application;

namespace Tollgate.Api.Contracts;

/// <param name="Mode">History is kept per mode; this says which one the numbers belong to.</param>
/// <param name="RetentionDays">Only scans of this many most recent days are counted.</param>
/// <param name="AverageScore">Absent while there is no history.</param>
/// <param name="DangerousRatio">Share (0 to 1) of scans with the Dangerous verdict; absent while there is no history.</param>
/// <param name="TopHosts">The most scanned hosts, busiest first.</param>
public sealed record StatsResponse(
    ScanMode Mode,
    int RetentionDays,
    int TotalScans,
    double? AverageScore,
    double? DangerousRatio,
    VerdictCounts Verdicts,
    IReadOnlyList<HostStatistics> TopHosts)
{
    public static StatsResponse From(ScanStatistics statistics, ScanMode mode, int retentionDays) => new(
        mode,
        retentionDays,
        statistics.TotalScans,
        statistics.AverageScore,
        statistics.DangerousRatio,
        statistics.Verdicts,
        statistics.TopHosts);
}
