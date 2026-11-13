// Port for aggregate numbers over the scan history (most scanned hosts, share of dangerous scans, ...).
namespace Tollgate.Application;

public interface IScanStatistics
{
    Task<ScanStatistics> GetAsync(CancellationToken cancellationToken);
}

/// <param name="AverageScore">Null while the history is empty: "no data" must not look like a score.</param>
/// <param name="TopHosts">The most scanned hosts, busiest first.</param>
public sealed record ScanStatistics(
    int TotalScans,
    double? AverageScore,
    VerdictCounts Verdicts,
    IReadOnlyList<HostStatistics> TopHosts)
{
    /// <summary>Share of scans (0 to 1) whose verdict was Dangerous; null while the history is empty.</summary>
    public double? DangerousRatio => TotalScans == 0 ? null : (double)Verdicts.Dangerous / TotalScans;
}

public sealed record VerdictCounts(int Dangerous, int Suspicious, int Safe);

public sealed record HostStatistics(string Host, int Scans, double AverageScore);
