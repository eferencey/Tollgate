// The three small shape checks: suspicious top-level domain, raw IP address, and the "user@host" trick.
using Tollgate.Domain;
using Tollgate.Scoring.Heuristics;

namespace Tollgate.Scoring.Tests.Heuristics;

public class UrlShapeTests
{
    private static readonly LocalAnalysisScoring Options = ScoringTestConfig.Options.LocalAnalysis;

    private static List<Finding> Check(IUrlHeuristic heuristic, string url) => heuristic.Evaluate(new Uri(url)).ToList();

    public class SuspiciousTld
    {
        private readonly SuspiciousTldHeuristic _heuristic = new(Options.SuspiciousTld);

        [Theory]
        [InlineData("https://free-prize.top/", "top")]
        [InlineData("https://sub.example.ZIP/x", "zip")] // Uri lower-cases hosts
        public void A_listed_top_level_domain_costs_its_penalty(string url, string tld)
        {
            var finding = Assert.Single(Check(_heuristic, url));

            Assert.Equal(ThreatSource.LocalAnalysis, finding.Source);
            Assert.Equal(Options.SuspiciousTld.Penalty, finding.PointsDeducted);
            Assert.Contains($".{tld}", finding.Description);
        }

        [Theory]
        [InlineData("https://example.com/")]
        [InlineData("https://top.example.com/")] // the word only counts as the last label
        [InlineData("https://free-bonus.xyz/claim")] // .xyz is common and legitimate; also keeps the 63 demo valid
        [InlineData("https://localhost/")]
        [InlineData("http://192.168.0.1/")]
        public void Other_hosts_have_no_finding(string url)
        {
            Assert.Empty(Check(_heuristic, url));
        }

        [Fact]
        public void The_list_comes_from_configuration()
        {
            var custom = new SuspiciousTldHeuristic(Options.SuspiciousTld with { Tlds = ["xyz"] });

            Assert.Single(Check(custom, "https://free-bonus.xyz/"));
            Assert.Empty(Check(custom, "https://free-prize.top/"));
        }
    }

    public class IpHost
    {
        private readonly IpHostHeuristic _heuristic = new(Options.IpHost);

        [Theory]
        [InlineData("http://192.168.0.1/login")]
        [InlineData("https://8.8.8.8:8443/")]
        [InlineData("http://[2001:db8::1]/")]
        [InlineData("http://3232235777/")] // 192.168.0.1 written as one decimal number
        public void A_raw_address_costs_its_penalty(string url)
        {
            var finding = Assert.Single(Check(_heuristic, url));

            Assert.Equal(Options.IpHost.Penalty, finding.PointsDeducted);
            Assert.Equal(ThreatSource.LocalAnalysis, finding.Source);
        }

        [Theory]
        [InlineData("https://example.com/")]
        [InlineData("https://192.example.com/")] // digits in a name are not an address
        [InlineData("https://example.com/?ip=192.168.0.1")]
        public void Names_have_no_finding(string url)
        {
            Assert.Empty(Check(_heuristic, url));
        }
    }

    public class UserInfo
    {
        private readonly UserInfoHeuristic _heuristic = new(Options.UserInfo);

        [Theory]
        [InlineData("http://google.com@evil.example/login")]
        [InlineData("https://user:pass@example.com/")]
        public void A_username_part_costs_its_penalty_and_names_the_real_host(string url)
        {
            var finding = Assert.Single(Check(_heuristic, url));

            Assert.Equal(Options.UserInfo.Penalty, finding.PointsDeducted);
            Assert.Contains(new Uri(url).Host, finding.Description);
        }

        [Theory]
        [InlineData("https://example.com/")]
        [InlineData("https://example.com/path?mail=someone@example.org")] // an @ after the host is just text
        public void Other_links_have_no_finding(string url)
        {
            Assert.Empty(Check(_heuristic, url));
        }
    }
}
