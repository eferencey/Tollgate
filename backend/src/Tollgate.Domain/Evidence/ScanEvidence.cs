// Raw facts gathered from all sources for one URL; the scoring engine's only input.
// A null member means that source did not respond (timeout, quota, error), which is
// different from "responded and found nothing".
namespace Tollgate.Domain.Evidence;

public sealed record ScanEvidence(
    VirusTotalEvidence? VirusTotal,
    SafeBrowsingEvidence? SafeBrowsing,
    UrlHausEvidence? UrlHaus,
    RdapEvidence? Rdap)
{
    /// <summary>
    /// The sources asked over the network, in display order. <see cref="ThreatSource.LocalAnalysis"/> is left out on
    /// purpose: it cannot fail, so counting it would make a total outage look like a partial answer.
    /// </summary>
    public static IReadOnlyList<ThreatSource> QueriedSources { get; } =
        [ThreatSource.VirusTotal, ThreatSource.SafeBrowsing, ThreatSource.UrlHaus, ThreatSource.Rdap];

    public IReadOnlyList<ThreatSource> RespondedSources =>
        QueriedSources.Where(HasResponded).ToList();

    public IReadOnlyList<ThreatSource> UnavailableSources =>
        QueriedSources.Where(source => !HasResponded(source)).ToList();

    private bool HasResponded(ThreatSource source) => source switch
    {
        ThreatSource.VirusTotal => VirusTotal is not null,
        ThreatSource.SafeBrowsing => SafeBrowsing is not null,
        ThreatSource.UrlHaus => UrlHaus is not null,
        ThreatSource.Rdap => Rdap is not null,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };
}
