// Engine-level behaviour: caps, explainability, floor at zero and missing sources.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Tests;

public class ScoringEngineTests
{
    private const string Url = "https://example.test/";
    private readonly ScoringEngine _engine = new(ScoringTestConfig.Options);

    [Theory]
    [InlineData(2, 45)] // 2 malicious engines: 100 - 24 = 76, capped to 45
    [InlineData(4, 45)] // 4 engines: 100 - 48 = 52, capped to 45
    [InlineData(5, 15)] // 5 engines: both caps apply, the lowest (15) wins
    public void Malicious_engine_count_caps_the_score(int malicious, int expectedScore)
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithVirusTotal(malicious: malicious));

        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(ThreatSource.VirusTotal, result.CappedBy?.Source);
    }

    [Fact]
    public void Single_malicious_engine_has_no_cap()
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithVirusTotal(malicious: 1));

        Assert.Equal(88, result.Score);
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void Safe_browsing_listing_caps_the_score_at_20()
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithSafeBrowsingListed());

        Assert.Equal(20, result.Score); // 100 - 60 = 40, capped to 20
        Assert.Equal(ThreatSource.SafeBrowsing, result.CappedBy?.Source);
    }

    [Fact]
    public void Active_urlhaus_url_caps_the_score_at_5()
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithUrlHaus(UrlHausUrlStatus.ActivelyDistributing));

        Assert.Equal(5, result.Score); // 100 - 80 = 20, capped to 5
        Assert.Equal(ThreatSource.UrlHaus, result.CappedBy?.Source);
    }

    [Fact]
    public void Past_urlhaus_url_caps_the_score_at_35()
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithUrlHaus(UrlHausUrlStatus.PastRecord));

        Assert.Equal(35, result.Score); // 100 - 40 = 60, capped to 35
        Assert.Equal(Verdict.Dangerous, result.Verdict);
    }

    [Fact]
    public void Lowest_cap_across_sources_wins()
    {
        // Penalties: 24 + 40 = 64 -> 36 before caps. Caps are 45 (VirusTotal) and 35 (URLhaus).
        var evidence = EvidenceBuilder.Clean() with
        {
            VirusTotal = new VirusTotalEvidence(MaliciousEngines: 2, SuspiciousEngines: 0),
            UrlHaus = new UrlHausEvidence(UrlHausUrlStatus.PastRecord, HostRecordCount: 0),
        };

        var result = _engine.Score(Url, evidence);

        Assert.Equal(35, result.Score);
        Assert.Equal(ThreatSource.UrlHaus, result.CappedBy?.Source);
    }

    [Fact]
    public void Cap_is_not_reported_when_penalties_already_push_the_score_below_it()
    {
        var evidence = EvidenceBuilder.Clean() with
        {
            SafeBrowsing = new SafeBrowsingEvidence(IsListed: true), // cap 20
            VirusTotal = new VirusTotalEvidence(MaliciousEngines: 0, SuspiciousEngines: 4), // -20
            UrlHaus = new UrlHausEvidence(UrlHausUrlStatus.PastRecord, HostRecordCount: 0), // -40
        };

        var result = _engine.Score(Url, evidence); // 100 - 60 - 20 - 40 = clamped to 0

        Assert.Equal(0, result.Score);
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void Score_never_drops_below_zero()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(100, 100),
            new SafeBrowsingEvidence(true),
            new UrlHausEvidence(UrlHausUrlStatus.ActivelyDistributing, 500),
            new RdapEvidence(0));

        Assert.Equal(0, _engine.Score(Url, evidence).Score);
    }

    [Fact]
    public void Every_deduction_is_listed_with_its_source()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(1, 1),
            new SafeBrowsingEvidence(false),
            new UrlHausEvidence(UrlHausUrlStatus.NotListed, 0),
            new RdapEvidence(10));

        var result = _engine.Score(Url, evidence);

        Assert.Collection(
            result.Findings,
            f => Assert.Equal((ThreatSource.VirusTotal, 12), (f.Source, f.PointsDeducted)),
            f => Assert.Equal((ThreatSource.VirusTotal, 5), (f.Source, f.PointsDeducted)),
            f => Assert.Equal((ThreatSource.Rdap, 15), (f.Source, f.PointsDeducted)));
        Assert.Equal(100 - result.Findings.Sum(f => f.PointsDeducted), result.Score);
    }

    [Fact]
    public void Result_carries_the_url_it_was_given()
    {
        Assert.Equal(Url, _engine.Score(Url, EvidenceBuilder.Clean()).Url);
    }

    // --- Missing sources -------------------------------------------------------------------

    [Fact]
    public void Unavailable_source_is_skipped_and_lowers_confidence()
    {
        var evidence = EvidenceBuilder.Clean() with { VirusTotal = null };

        var result = _engine.Score(Url, evidence);

        Assert.Equal(100, result.Score);
        Assert.Equal(Confidence.Medium, result.Confidence);
        Assert.Equal([ThreatSource.VirusTotal], result.UnavailableSources);
    }

    [Fact]
    public void Remaining_sources_still_score_when_one_is_unavailable()
    {
        var evidence = EvidenceBuilder.WithSafeBrowsingListed() with { UrlHaus = null };

        var result = _engine.Score(Url, evidence);

        Assert.Equal(20, result.Score);
        Assert.Equal(Confidence.Medium, result.Confidence);
        Assert.Equal([ThreatSource.UrlHaus], result.UnavailableSources);
    }

    [Fact]
    public void Only_one_source_responding_gives_low_confidence()
    {
        var evidence = new ScanEvidence(null, new SafeBrowsingEvidence(false), null, null);

        var result = _engine.Score(Url, evidence);

        Assert.Equal(Confidence.Low, result.Confidence);
        Assert.Equal(3, result.UnavailableSources.Count);
    }

    [Fact]
    public void No_source_responding_gives_low_confidence_and_lists_all_sources_unavailable()
    {
        var result = _engine.Score(Url, new ScanEvidence(null, null, null, null));

        Assert.Equal(Confidence.Low, result.Confidence);
        Assert.Equal(ScanEvidence.QueriedSources, result.UnavailableSources);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void Rdap_answering_without_a_registration_date_counts_as_responded_but_costs_nothing()
    {
        var result = _engine.Score(Url, EvidenceBuilder.WithDomainAge(null));

        Assert.Equal(100, result.Score);
        Assert.Equal(Confidence.High, result.Confidence);
        Assert.Empty(result.UnavailableSources);
    }
}
