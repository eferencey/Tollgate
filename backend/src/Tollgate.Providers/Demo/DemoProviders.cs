// Fake sources for demo mode (no API keys). Same IThreatProvider contract as the real ones, so the
// orchestrator and scoring engine cannot tell the difference. Numbers match the original spec's examples.
using System.Collections.Concurrent;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.Demo;

public sealed class DemoVirusTotalProvider : IThreatProvider<VirusTotalEvidence>
{
    // Remembers which "slow" URLs were already asked once, so the second request finds the analysis finished.
    private readonly ConcurrentDictionary<string, bool> alreadyAsked = new();

    public ThreatSource Source => ThreatSource.VirusTotal;

    public Task<ProviderResult<VirusTotalEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        var result = DemoScenarios.For(url) switch
        {
            DemoScenario.Malicious => ProviderResult<VirusTotalEvidence>.Completed(new(MaliciousEngines: 20, SuspiciousEngines: 3)),
            DemoScenario.Suspicious => ProviderResult<VirusTotalEvidence>.Completed(new(MaliciousEngines: 1, SuspiciousEngines: 2)),
            DemoScenario.SlowVirusTotal when alreadyAsked.TryAdd(url.AbsoluteUri, true) => ProviderResult<VirusTotalEvidence>.Pending(),
            _ => ProviderResult<VirusTotalEvidence>.Completed(new(MaliciousEngines: 0, SuspiciousEngines: 0)),
        };
        return Task.FromResult(result);
    }
}

public sealed class DemoSafeBrowsingProvider : IThreatProvider<SafeBrowsingEvidence>
{
    public ThreatSource Source => ThreatSource.SafeBrowsing;

    public Task<ProviderResult<SafeBrowsingEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        var result = DemoScenarios.For(url) switch
        {
            DemoScenario.PartialOutage => ProviderResult<SafeBrowsingEvidence>.Unavailable("Demo: simulated outage"),
            DemoScenario.Malicious => ProviderResult<SafeBrowsingEvidence>.Completed(new(IsListed: true)),
            _ => ProviderResult<SafeBrowsingEvidence>.Completed(new(IsListed: false)),
        };
        return Task.FromResult(result);
    }
}

public sealed class DemoUrlHausProvider : IThreatProvider<UrlHausEvidence>
{
    public ThreatSource Source => ThreatSource.UrlHaus;

    public Task<ProviderResult<UrlHausEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        var evidence = DemoScenarios.For(url) switch
        {
            DemoScenario.Malicious => new UrlHausEvidence(UrlHausUrlStatus.ActivelyDistributing, HostRecordCount: 12),
            DemoScenario.Suspicious => new UrlHausEvidence(UrlHausUrlStatus.NotListed, HostRecordCount: 1),
            _ => new UrlHausEvidence(UrlHausUrlStatus.NotListed, HostRecordCount: 0),
        };
        return Task.FromResult(ProviderResult<UrlHausEvidence>.Completed(evidence));
    }
}

public sealed class DemoRdapProvider : IThreatProvider<RdapEvidence>
{
    public ThreatSource Source => ThreatSource.Rdap;

    public Task<ProviderResult<RdapEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        var result = DemoScenarios.For(url) switch
        {
            DemoScenario.PartialOutage => ProviderResult<RdapEvidence>.Unavailable("Demo: simulated outage"),
            DemoScenario.Malicious => ProviderResult<RdapEvidence>.Completed(new(DomainAgeDays: 10)),
            DemoScenario.Suspicious => ProviderResult<RdapEvidence>.Completed(new(DomainAgeDays: 90)),
            _ => ProviderResult<RdapEvidence>.Completed(new(DomainAgeDays: 3650)),
        };
        return Task.FromResult(result);
    }
}
