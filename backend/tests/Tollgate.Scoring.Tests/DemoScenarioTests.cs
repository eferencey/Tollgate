// The three demo results from the original presentation (100 / 63 / 0) must come out exactly the same.
// The presentation gives only prose for each scenario, so the exact evidence below is our documented reading of it:
// the "bonus" link is 12 (1 malicious) + 10 (2 suspicious) + 5 (domain < 180 days) + 10 (1 URLhaus host record) = 37.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Tests;

public class DemoScenarioTests
{
    private const string Url = "https://example.test/";
    private readonly ScoringEngine _engine = new(ScoringTestConfig.Options);

    [Fact]
    public void Established_clean_domain_scores_100_and_is_safe()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(0, 0),
            new SafeBrowsingEvidence(false),
            new UrlHausEvidence(UrlHausUrlStatus.NotListed, 0),
            new RdapEvidence(DomainAgeDays: 9000));

        var result = _engine.Score(Url, evidence);

        Assert.Equal(100, result.Score);
        Assert.Equal(Verdict.Safe, result.Verdict);
        Assert.Equal(Confidence.High, result.Confidence);
        Assert.Empty(result.Findings);
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void Free_bonus_link_scores_63_and_is_suspicious()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(MaliciousEngines: 1, SuspiciousEngines: 2),
            new SafeBrowsingEvidence(false),
            new UrlHausEvidence(UrlHausUrlStatus.NotListed, HostRecordCount: 1),
            new RdapEvidence(DomainAgeDays: 60));

        var result = _engine.Score(Url, evidence);

        Assert.Equal(63, result.Score);
        Assert.Equal(Verdict.Suspicious, result.Verdict);
        Assert.Equal(Confidence.High, result.Confidence);
        Assert.Equal(37, result.Findings.Sum(f => f.PointsDeducted));
        Assert.Null(result.CappedBy);
    }

    [Fact]
    public void Exe_download_link_scores_0_and_is_dangerous()
    {
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(MaliciousEngines: 30, SuspiciousEngines: 6),
            new SafeBrowsingEvidence(true),
            new UrlHausEvidence(UrlHausUrlStatus.ActivelyDistributing, HostRecordCount: 4),
            new RdapEvidence(DomainAgeDays: 3));

        var result = _engine.Score(Url, evidence);

        Assert.Equal(0, result.Score);
        Assert.Equal(Verdict.Dangerous, result.Verdict);
        Assert.Equal(Confidence.High, result.Confidence);
    }
}
