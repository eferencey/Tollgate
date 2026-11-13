// How local analysis meets the score formula: it adds to the same deductions, never counts as a source,
// never sets a cap, and leaves the original presentation's 100 / 63 / 0 untouched.
// U+0430 below is Cyrillic "a" (it looks exactly like the Latin one).
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Tests.Heuristics;

public class LocalAnalysisEngineTests
{
    private static readonly LocalAnalysisScoring Local = ScoringTestConfig.Options.LocalAnalysis;
    private readonly ScoringEngine _engine = new(ScoringTestConfig.Options);

    [Fact]
    public void A_look_alike_domain_loses_points_even_when_every_source_is_clean()
    {
        var result = _engine.Score("https://paypa1.com/", EvidenceBuilder.Clean());

        var finding = Assert.Single(result.Findings);
        Assert.Equal(ThreatSource.LocalAnalysis, finding.Source);
        Assert.Equal(100 - Local.Typosquatting.SubstitutionPenalty, result.Score);
        Assert.Equal(Verdict.Suspicious, result.Verdict);
        Assert.Equal(Confidence.High, result.Confidence);
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void An_ordinary_link_has_no_local_findings()
    {
        var result = _engine.Score("https://example.com/path?q=1", EvidenceBuilder.Clean());

        Assert.Empty(result.Findings);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public void Local_deductions_add_to_the_deductions_of_the_sources()
    {
        var result = _engine.Score("https://paypa1.com/", EvidenceBuilder.WithVirusTotal(malicious: 1));

        var expected = 100 - ScoringTestConfig.Options.VirusTotal.MaliciousPenaltyPerEngine - Local.Typosquatting.SubstitutionPenalty;
        Assert.Equal(expected, result.Score);
        Assert.Equal([ThreatSource.VirusTotal, ThreatSource.LocalAnalysis], result.Findings.Select(f => f.Source));
    }

    [Fact]
    public void Local_analysis_is_not_a_source_so_it_never_changes_confidence()
    {
        var nobodyAnswered = new ScanEvidence(null, null, null, null);

        var result = _engine.Score("https://paypa1.com/", nobodyAnswered);

        Assert.Equal(Confidence.Low, result.Confidence);
        Assert.Equal(ScanEvidence.QueriedSources, result.UnavailableSources);
        Assert.Single(result.Findings); // the local check still ran
    }

    [Theory]
    [InlineData("http://\U00000430pple.top/")] // mixed alphabets + imitation of apple.com + suspicious TLD
    [InlineData("http://apple.com@192.168.0.1/")] // "@" trick + raw IP address
    public void Local_findings_never_set_a_score_cap_so_only_threat_intelligence_can_cap(string url)
    {
        var result = _engine.Score(url, EvidenceBuilder.Clean());

        Assert.True(result.Findings.Count >= 2, "the URL was chosen to trigger several checks at once");
        Assert.All(result.Findings, f => Assert.Equal(ThreatSource.LocalAnalysis, f.Source));
        Assert.All(result.Findings, f => Assert.Null(f.ScoreCap));
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void Heavy_local_findings_can_still_reach_dangerous_but_never_go_below_zero()
    {
        var result = _engine.Score("http://\U00000430pple.top/", EvidenceBuilder.WithSafeBrowsingListed());

        Assert.InRange(result.Score, ScoringEngine.MinScore, ScoringEngine.MaxScore);
        Assert.Equal(Verdict.Dangerous, result.Verdict);
    }

    [Fact]
    public void The_presentations_bonus_link_still_scores_63_because_xyz_is_not_a_listed_tld()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(MaliciousEngines: 1, SuspiciousEngines: 2),
            new SafeBrowsingEvidence(false),
            new UrlHausEvidence(UrlHausUrlStatus.NotListed, HostRecordCount: 1),
            new RdapEvidence(DomainAgeDays: 60));

        var result = _engine.Score("http://free-bonus.xyz/claim", evidence);

        Assert.Equal(63, result.Score);
        Assert.DoesNotContain(result.Findings, f => f.Source == ThreatSource.LocalAnalysis);
    }
}
