// Per-rule penalty arithmetic, maximums and boundaries, driven by the real configuration.
using Tollgate.Domain.Evidence;
using Tollgate.Scoring.Rules;

namespace Tollgate.Scoring.Tests;

public class RuleTests
{
    private static readonly ScoringOptions Options = ScoringTestConfig.Options;

    [Theory]
    [InlineData(1, 12)]
    [InlineData(3, 36)]
    [InlineData(5, 60)]
    [InlineData(6, 60)] // -72 would exceed the maximum
    [InlineData(40, 60)]
    public void Malicious_engines_cost_12_each_up_to_60(int engines, int expectedPenalty)
    {
        var findings = new VirusTotalRule(Options.VirusTotal).Evaluate(EvidenceBuilder.WithVirusTotal(malicious: engines));

        Assert.Equal(expectedPenalty, Assert.Single(findings).PointsDeducted);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(4, 20)]
    [InlineData(5, 20)] // -25 would exceed the maximum
    [InlineData(30, 20)]
    public void Suspicious_engines_cost_5_each_up_to_20_and_never_cap(int engines, int expectedPenalty)
    {
        var finding = Assert.Single(new VirusTotalRule(Options.VirusTotal).Evaluate(EvidenceBuilder.WithVirusTotal(suspicious: engines)));

        Assert.Equal(expectedPenalty, finding.PointsDeducted);
        Assert.Null(finding.ScoreCap);
    }

    [Fact]
    public void Clean_virustotal_result_produces_no_findings()
    {
        Assert.Empty(new VirusTotalRule(Options.VirusTotal).Evaluate(EvidenceBuilder.Clean()));
    }

    [Theory]
    [InlineData(1, 10)] // 8 + 2 x 1
    [InlineData(3, 14)]
    [InlineData(11, 30)] // 8 + 22 = 30, exactly the maximum
    [InlineData(12, 30)] // 32 would exceed the maximum
    [InlineData(500, 30)]
    public void Urlhaus_host_records_cost_8_plus_2_per_record_up_to_30(int records, int expectedPenalty)
    {
        var findings = new UrlHausRule(Options.UrlHaus).Evaluate(EvidenceBuilder.WithUrlHaus(hostRecords: records));

        var finding = Assert.Single(findings);
        Assert.Equal(expectedPenalty, finding.PointsDeducted);
        Assert.Null(finding.ScoreCap);
    }

    [Fact]
    public void Urlhaus_url_status_and_host_records_are_independent_findings()
    {
        var evidence = EvidenceBuilder.WithUrlHaus(UrlHausUrlStatus.ActivelyDistributing, hostRecords: 2);

        var findings = new UrlHausRule(Options.UrlHaus).Evaluate(evidence).ToList();

        Assert.Equal([80, 12], findings.Select(f => f.PointsDeducted));
    }

    [Fact]
    public void Urlhaus_with_nothing_found_produces_no_findings()
    {
        Assert.Empty(new UrlHausRule(Options.UrlHaus).Evaluate(EvidenceBuilder.Clean()));
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(29, 15)]
    [InlineData(30, 5)] // 30 is no longer "< 30 days"
    [InlineData(179, 5)]
    public void Young_domains_are_penalized_in_two_tiers(int ageDays, int expectedPenalty)
    {
        var findings = new DomainAgeRule(Options.DomainAge).Evaluate(EvidenceBuilder.WithDomainAge(ageDays));

        Assert.Equal(expectedPenalty, Assert.Single(findings).PointsDeducted);
    }

    [Theory]
    [InlineData(180)] // 180 is no longer "< 180 days"
    [InlineData(5000)]
    [InlineData(null)] // registry publishes no date
    public void Old_or_undated_domains_are_not_penalized(int? ageDays)
    {
        Assert.Empty(new DomainAgeRule(Options.DomainAge).Evaluate(EvidenceBuilder.WithDomainAge(ageDays)));
    }

    [Fact]
    public void Safe_browsing_not_listed_produces_no_findings()
    {
        Assert.Empty(new SafeBrowsingRule(Options.SafeBrowsing).Evaluate(EvidenceBuilder.Clean()));
    }
}
