// Settings for the VirusTotal v3 provider, bound from "Providers:VirusTotal". No defaults: appsettings.json is the source of truth.
namespace Tollgate.Providers.VirusTotal;

public sealed record VirusTotalOptions
{
    public const string SectionName = "Providers:VirusTotal";

    /// <summary>Comes from the VT_API_KEY environment variable; empty = provider reports "unavailable".</summary>
    public string? ApiKey { get; init; }

    /// <summary>Must end with a slash, otherwise relative paths replace the last segment.</summary>
    public required Uri BaseAddress { get; init; }

    /// <summary>
    /// How often to ask for a freshly submitted URL's analysis before giving up with "pending".
    /// Bounded so one request never hangs; the UI re-polls afterwards.
    /// </summary>
    public required int AnalysisPollAttempts { get; init; }

    public required TimeSpan AnalysisPollDelay { get; init; }
}
