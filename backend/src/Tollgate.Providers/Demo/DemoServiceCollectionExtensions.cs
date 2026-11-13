// Registers the fake sources instead of the real ones (demo mode: no API keys configured).
using Microsoft.Extensions.DependencyInjection;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.Demo;

public static class DemoServiceCollectionExtensions
{
    public static IServiceCollection AddDemoThreatProviders(this IServiceCollection services)
    {
        // Singletons: the demo VirusTotal provider remembers which "slow" URLs it was already asked about.
        services.AddSingleton<IThreatProvider<VirusTotalEvidence>, DemoVirusTotalProvider>();
        services.AddSingleton<IThreatProvider<SafeBrowsingEvidence>, DemoSafeBrowsingProvider>();
        services.AddSingleton<IThreatProvider<UrlHausEvidence>, DemoUrlHausProvider>();
        services.AddSingleton<IThreatProvider<RdapEvidence>, DemoRdapProvider>();
        return services;
    }
}
