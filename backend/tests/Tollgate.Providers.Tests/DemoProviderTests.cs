// The demo sources: scenario selection by URL shape and the "slow VirusTotal" two-step behaviour.
using Tollgate.Providers.Demo;

namespace Tollgate.Providers.Tests;

public class DemoProviderTests
{
    [Theory]
    [InlineData("https://example.com/", DemoScenario.Clean)]
    [InlineData("https://free-bonus.com/win", DemoScenario.Suspicious)]
    [InlineData("https://prizes.xyz/", DemoScenario.Suspicious)]
    [InlineData("https://downloads.example.org/setup.EXE", DemoScenario.Malicious)]
    [InlineData("https://malware-test.com/", DemoScenario.Malicious)]
    [InlineData("https://slow-site.com/", DemoScenario.SlowVirusTotal)]
    [InlineData("https://outage.example/", DemoScenario.PartialOutage)]
    public void Scenario_is_chosen_from_the_url_shape(string url, DemoScenario expected) =>
        Assert.Equal(expected, DemoScenarios.For(new Uri(url)));

    [Fact]
    public void The_most_severe_hint_wins_when_several_match()
    {
        Assert.Equal(DemoScenario.Malicious, DemoScenarios.For(new Uri("https://bonus.xyz/free.exe")));
    }

    [Fact]
    public async Task Slow_virustotal_is_pending_once_then_completes()
    {
        var provider = new DemoVirusTotalProvider();
        var url = new Uri("https://slow-site.com/");

        var first = await provider.CheckAsync(url, CancellationToken.None);
        var second = await provider.CheckAsync(url, CancellationToken.None);

        Assert.Equal(ProviderStatus.Pending, first.Status);
        Assert.Equal(ProviderStatus.Completed, second.Status);
    }

    [Fact]
    public async Task Partial_outage_makes_safe_browsing_and_rdap_unavailable_but_not_the_others()
    {
        var url = new Uri("https://outage.example/");

        Assert.Equal(ProviderStatus.Unavailable, (await new DemoSafeBrowsingProvider().CheckAsync(url, CancellationToken.None)).Status);
        Assert.Equal(ProviderStatus.Unavailable, (await new DemoRdapProvider().CheckAsync(url, CancellationToken.None)).Status);
        Assert.Equal(ProviderStatus.Completed, (await new DemoVirusTotalProvider().CheckAsync(url, CancellationToken.None)).Status);
        Assert.Equal(ProviderStatus.Completed, (await new DemoUrlHausProvider().CheckAsync(url, CancellationToken.None)).Status);
    }
}
