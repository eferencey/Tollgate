// Registers the four providers: one line each, every provider with its own HttpClient and its own
// resilience pipeline (timeout + retry + circuit breaker), so one slow source cannot affect another.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tollgate.Domain.Evidence;
using Tollgate.Providers.Rdap;
using Tollgate.Providers.SafeBrowsing;
using Tollgate.Providers.UrlHaus;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Providers;

public static class ProviderServiceCollectionExtensions
{
    public const string ResilienceSection = "Providers:Resilience";
    private const string UserAgent = "Tollgate/1.0";

    public static IServiceCollection AddThreatProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        AddProvider<IThreatProvider<VirusTotalEvidence>, VirusTotalProvider, VirusTotalOptions>(services, configuration, VirusTotalOptions.SectionName);
        AddProvider<IThreatProvider<SafeBrowsingEvidence>, SafeBrowsingProvider, SafeBrowsingOptions>(services, configuration, SafeBrowsingOptions.SectionName);
        AddProvider<IThreatProvider<UrlHausEvidence>, UrlHausProvider, UrlHausOptions>(services, configuration, UrlHausOptions.SectionName);
        AddProvider<IThreatProvider<RdapEvidence>, RdapProvider, RdapOptions>(services, configuration, RdapOptions.SectionName);

        return services;
    }

    private static void AddProvider<TService, TProvider, TOptions>(
        IServiceCollection services, IConfiguration configuration, string optionsSection)
        where TService : class
        where TProvider : class, TService
        where TOptions : class
    {
        services.AddOptions<TOptions>().Bind(configuration.GetSection(optionsSection));
        // .NET sends no User-Agent by default and rdap.org answers such requests with 403, so every provider identifies itself.
        services.AddHttpClient<TService, TProvider>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent))
            .AddStandardResilienceHandler(configuration.GetSection(ResilienceSection));
    }
}
