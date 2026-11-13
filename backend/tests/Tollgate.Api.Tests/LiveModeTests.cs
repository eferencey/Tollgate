// Live mode (at least one key configured): the real providers are wired and a source without a key is
// "unavailable" instead of being faked. Network-facing providers are replaced by scripted ones.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;
using Tollgate.Providers;
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class LiveModeTests
{
    private static TollgateFactory LiveFactoryWithFakeNetworkSources() =>
        new TollgateFactory()
            .With("GSB_API_KEY", "test-key") // one key is enough to leave demo mode
            .WithServices(services =>
            {
                services.RemoveAll<IThreatProvider<SafeBrowsingEvidence>>();
                services.RemoveAll<IThreatProvider<UrlHausEvidence>>();
                services.RemoveAll<IThreatProvider<RdapEvidence>>();
                services.AddSingleton<IThreatProvider<SafeBrowsingEvidence>>(new FixedProvider<SafeBrowsingEvidence>(ThreatSource.SafeBrowsing, new SafeBrowsingEvidence(IsListed: true)));
                services.AddSingleton<IThreatProvider<UrlHausEvidence>>(new FixedProvider<UrlHausEvidence>(ThreatSource.UrlHaus, new UrlHausEvidence(UrlHausUrlStatus.NotListed, 0)));
                services.AddSingleton<IThreatProvider<RdapEvidence>>(new FixedProvider<RdapEvidence>(ThreatSource.Rdap, new RdapEvidence(3650)));
            });

    [Fact]
    public async Task One_configured_key_switches_to_live_mode_and_the_response_says_so()
    {
        using var factory = LiveFactoryWithFakeNetworkSources();

        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("example.com"));

        Assert.Equal("Live", body.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task A_source_without_its_key_is_unavailable_in_live_mode_not_faked()
    {
        using var factory = LiveFactoryWithFakeNetworkSources();

        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("example.com"));

        var virusTotal = body.GetProperty("sources").EnumerateArray().Single(s => s.GetProperty("source").GetString() == "VirusTotal");
        Assert.Equal("Unavailable", virusTotal.GetProperty("state").GetString());
        Assert.Contains("API key", virusTotal.GetProperty("reason").GetString());
        Assert.Equal("Medium", body.GetProperty("confidence").GetString());
    }

    [Fact]
    public async Task Live_and_demo_modes_use_the_same_scoring_code()
    {
        // Safe Browsing says "listed" (-60, cap 20); the same engine that scores demo data scores this too.
        using var factory = LiveFactoryWithFakeNetworkSources();

        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("example.com"));

        Assert.Equal(20, body.GetProperty("score").GetInt32());
        Assert.Equal("Dangerous", body.GetProperty("verdict").GetString());
        Assert.Equal("SafeBrowsing", body.GetProperty("cappedBy").GetProperty("source").GetString());
    }

    private sealed class FixedProvider<TEvidence>(ThreatSource source, TEvidence evidence) : IThreatProvider<TEvidence>
        where TEvidence : class
    {
        public ThreatSource Source => source;

        public Task<ProviderResult<TEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken) =>
            Task.FromResult(ProviderResult<TEvidence>.Completed(evidence));
    }
}
