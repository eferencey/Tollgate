// Free-tier limits of the VirusTotal public API (4 requests/min, 500/day), bound from "VirusTotalQueue".
using System.ComponentModel.DataAnnotations;

namespace Tollgate.Infrastructure.VirusTotal;

public sealed record VirusTotalQueueOptions
{
    public const string SectionName = "VirusTotalQueue";

    /// <summary>Minimum time between two requests; 15 s keeps us under 4 per minute.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:10:00")]
    public required TimeSpan MinSpacing { get; init; }

    /// <summary>Requests allowed per UTC day (VirusTotal resets its counter at 00:00 UTC).</summary>
    [Range(1, 1_000_000)]
    public required int DailyLimit { get; init; }

    /// <summary>
    /// Longest a request may wait for its turn. Beyond this the queue counts as full, so a burst of scans
    /// cannot park HTTP requests for minutes.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:10:00")]
    public required TimeSpan MaxWait { get; init; }
}
