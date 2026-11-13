// Tunable numbers of the scan flow, bound from the "Scan" section of appsettings.json. No defaults on purpose.
using System.ComponentModel.DataAnnotations;

namespace Tollgate.Application;

public sealed record ScanOptions
{
    public const string SectionName = "Scan";

    /// <summary>How long gathered evidence for a URL is reused (scan flow step 2: 60 minutes).</summary>
    [Range(typeof(TimeSpan), "00:00:01", "23:59:59")]
    public required TimeSpan CacheDuration { get; init; }

    /// <summary>Suggested wait before asking again when a source (VirusTotal) is still analysing.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public required TimeSpan PollInterval { get; init; }
}
