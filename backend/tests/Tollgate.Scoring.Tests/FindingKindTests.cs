// Every kind of finding must come out of the engine under its own stable code, because the UI keys its
// plain-language explanations on the code and never on the (reworded-at-will) description.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Tests;

public class FindingKindTests
{
    private const string CleanUrl = "https://example.com/";
    private readonly ScoringEngine _engine = new(ScoringTestConfig.Options);

    private IEnumerable<FindingKind> KindsOf(string url, ScanEvidence evidence) => _engine.Score(url, evidence).Findings.Select(f => f.Kind);

    [Fact]
    public void Source_findings_carry_the_kind_of_the_rule_that_produced_them()
    {
        Assert.Equal([FindingKind.EnginesMalicious, FindingKind.EnginesSuspicious], KindsOf(CleanUrl, EvidenceBuilder.WithVirusTotal(malicious: 1, suspicious: 1)));
        Assert.Equal([FindingKind.SafeBrowsingListed], KindsOf(CleanUrl, EvidenceBuilder.WithSafeBrowsingListed()));
        Assert.Equal([FindingKind.DomainRecentlyRegistered], KindsOf(CleanUrl, EvidenceBuilder.WithDomainAge(5)));
    }

    [Fact]
    public void The_three_urlhaus_findings_are_told_apart()
    {
        Assert.Equal(
            [FindingKind.UrlActivelyDistributingMalware, FindingKind.DomainHasMalwareRecords],
            KindsOf(CleanUrl, EvidenceBuilder.WithUrlHaus(UrlHausUrlStatus.ActivelyDistributing, hostRecords: 2)));
        Assert.Equal([FindingKind.UrlDistributedMalwareInPast], KindsOf(CleanUrl, EvidenceBuilder.WithUrlHaus(UrlHausUrlStatus.PastRecord)));
    }

    [Theory]
    [InlineData("http://аpple.com/", FindingKind.MixedAlphabetDomain)] // U+0430 is Cyrillic "a"
    [InlineData("http://пример.com/", FindingKind.NonLatinAlphabetDomain)]
    [InlineData("http://paypa1.com/", FindingKind.BrandLookalikeCharacters)]
    [InlineData("http://paypall.com/", FindingKind.BrandTypo)]
    [InlineData("http://free-prize.top/", FindingKind.SuspiciousTld)]
    [InlineData("http://192.168.0.1/", FindingKind.RawIpAddressHost)]
    [InlineData("http://google.com@example.org/", FindingKind.HiddenBehindUserInfo)]
    public void Local_findings_carry_the_kind_of_the_check_that_produced_them(string url, FindingKind expected)
    {
        Assert.Contains(expected, KindsOf(url, EvidenceBuilder.Clean()));
    }

    [Fact]
    public void Every_kind_is_covered_by_a_test_here()
    {
        // Guards against adding a kind to the enum without a rule (and test) that emits it.
        var covered = new HashSet<FindingKind>
        {
            FindingKind.EnginesMalicious, FindingKind.EnginesSuspicious, FindingKind.SafeBrowsingListed,
            FindingKind.UrlActivelyDistributingMalware, FindingKind.UrlDistributedMalwareInPast, FindingKind.DomainHasMalwareRecords,
            FindingKind.DomainRecentlyRegistered, FindingKind.MixedAlphabetDomain, FindingKind.NonLatinAlphabetDomain,
            FindingKind.BrandLookalikeCharacters, FindingKind.BrandTypo, FindingKind.SuspiciousTld,
            FindingKind.RawIpAddressHost, FindingKind.HiddenBehindUserInfo,
        };

        Assert.Equal(Enum.GetValues<FindingKind>().ToHashSet(), covered);
    }
}
