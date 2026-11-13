// The composition root for the backend: the only place that picks concrete classes (live or demo sources,
// SQLite persistence, VirusTotal queue) and binds + validates every settings section at startup.
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Tollgate.Application;
using Tollgate.Infrastructure.Persistence;
using Tollgate.Infrastructure.VirusTotal;
using Tollgate.Providers;
using Tollgate.Providers.Demo;
using Tollgate.Providers.Rdap;
using Tollgate.Providers.SafeBrowsing;
using Tollgate.Providers.UrlHaus;
using Tollgate.Providers.VirusTotal;
using Tollgate.Scoring;

namespace Tollgate.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTollgateBackend(this IServiceCollection services, IConfiguration configuration, ScanMode mode)
    {
        services.AddSingleton(new ScanEnvironment(mode));
        services.AddSingleton(TimeProvider.System);

        AddValidatedOptions(services, configuration);

        // Same orchestrator and scoring code in both modes; only the four sources differ.
        if (mode == ScanMode.Live)
        {
            services.AddThreatProviders(configuration);
            AddValidatedProviderOptions(services, configuration);
        }
        else
        {
            services.AddDemoThreatProviders();
        }

        services.AddSingleton<IVirusTotalRequestGate, VirusTotalRequestQueue>();
        services.AddPersistence(provider => provider.GetRequiredService<IOptions<PersistenceOptions>>().Value
            .DatabasePath(provider.GetRequiredService<IHostEnvironment>().ContentRootPath, mode));
        services.AddSingleton(provider => new ScoringEngine(provider.GetRequiredService<IOptions<ScoringOptions>>().Value));

        // Scoped, not singleton: the real providers are typed HttpClients, which must not outlive a request scope.
        services.AddScoped<ScanOrchestrator>();

        return services;
    }

    private static void AddValidatedOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ScoringOptions>, ScoringOptionsValidator>();
        services.AddOptions<ScoringOptions>()
            .Bind(configuration.GetSection(ScoringOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<ScanOptions>()
            .Bind(configuration.GetSection(ScanOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<HistoryOptions>()
            .Bind(configuration.GetSection(HistoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<VirusTotalQueueOptions>()
            .Bind(configuration.GetSection(VirusTotalQueueOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ApiRateLimitOptions>()
            .Bind(configuration.GetSection(ApiRateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    // The providers bind their own options (AddThreatProviders); here we only add the startup checks.
    private static void AddValidatedProviderOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ProviderOptionsValidator>();
        services.AddSingleton<IValidateOptions<VirusTotalOptions>>(p => p.GetRequiredService<ProviderOptionsValidator>());
        services.AddSingleton<IValidateOptions<SafeBrowsingOptions>>(p => p.GetRequiredService<ProviderOptionsValidator>());
        services.AddSingleton<IValidateOptions<UrlHausOptions>>(p => p.GetRequiredService<ProviderOptionsValidator>());
        services.AddSingleton<IValidateOptions<RdapOptions>>(p => p.GetRequiredService<ProviderOptionsValidator>());

        services.AddOptions<VirusTotalOptions>().ValidateOnStart();
        services.AddOptions<SafeBrowsingOptions>().ValidateOnStart();
        services.AddOptions<UrlHausOptions>().ValidateOnStart();
        services.AddOptions<RdapOptions>().ValidateOnStart();

        // The retry/timeout/circuit-breaker numbers are applied lazily on the first HTTP call, which would
        // be too late; binding them once more under a throwaway name checks them with the same attributes.
        services.AddOptions<HttpStandardResilienceOptions>("startup-check")
            .Bind(configuration.GetSection(ProviderServiceCollectionExtensions.ResilienceSection))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
