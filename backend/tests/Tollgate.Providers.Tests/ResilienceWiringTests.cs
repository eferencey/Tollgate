// Verifies the real DI wiring (AddThreatProviders): every provider resolves, transient failures are retried
// and a hanging source times out into "unavailable". Only the network itself is faked.
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Tollgate.Domain.Evidence;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Providers.Tests;

public class ResilienceWiringTests
{
    private static readonly Uri Target = new("https://example.com/");

    private static ServiceProvider BuildServices(HttpMessageHandler fakeNetwork)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Providers:Resilience:AttemptTimeout:Timeout"] = "00:00:00.200",
                ["Providers:Resilience:TotalRequestTimeout:Timeout"] = "00:00:02",
                ["Providers:Resilience:Retry:MaxRetryAttempts"] = "2",
                ["Providers:Resilience:Retry:Delay"] = "00:00:00.001",
                ["Providers:VirusTotal:BaseAddress"] = "https://vt.test/api/v3/",
                ["Providers:VirusTotal:ApiKey"] = "test-key",
                ["Providers:VirusTotal:AnalysisPollAttempts"] = "1",
                ["Providers:VirusTotal:AnalysisPollDelay"] = "00:00:00",
                ["Providers:SafeBrowsing:BaseAddress"] = "https://gsb.test/v4/",
                ["Providers:SafeBrowsing:ApiKey"] = "test-key",
                ["Providers:UrlHaus:BaseAddress"] = "https://urlhaus.test/v1/",
                ["Providers:UrlHaus:AuthKey"] = "test-key",
                ["Providers:Rdap:BaseAddress"] = "https://rdap.test/",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddThreatProviders(configuration);
        services.AddSingleton<IVirusTotalRequestGate, FakeVirusTotalGate>();

        // Swap only the bottom of the HttpClient pipeline; the resilience handlers above it stay real.
        services.ConfigureAll<HttpClientFactoryOptions>(o =>
            o.HttpMessageHandlerBuilderActions.Add(builder => builder.PrimaryHandler = fakeNetwork));

        return services.BuildServiceProvider();
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        public int Calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void All_four_providers_resolve_from_the_container()
    {
        using var services = BuildServices(new StubHttpHandler(_ => StubHttpHandler.Json("{}")));

        Assert.NotNull(services.GetRequiredService<IThreatProvider<VirusTotalEvidence>>());
        Assert.NotNull(services.GetRequiredService<IThreatProvider<SafeBrowsingEvidence>>());
        Assert.NotNull(services.GetRequiredService<IThreatProvider<UrlHausEvidence>>());
        Assert.NotNull(services.GetRequiredService<IThreatProvider<RdapEvidence>>());
    }

    [Fact]
    public async Task Transient_server_errors_are_retried_until_the_source_recovers()
    {
        var calls = 0;
        var network = new StubHttpHandler(_ => ++calls < 3
            ? StubHttpHandler.Status(HttpStatusCode.ServiceUnavailable)
            : StubHttpHandler.Json("""{"events":[{"eventAction":"registration","eventDate":"2020-01-01T00:00:00Z"}]}"""));
        using var services = BuildServices(network);

        var result = await services.GetRequiredService<IThreatProvider<RdapEvidence>>()
            .CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(3, network.Requests.Count);
    }

    [Fact]
    public async Task Persistent_server_errors_end_as_unavailable_after_the_retry_budget()
    {
        var network = new StubHttpHandler(_ => StubHttpHandler.Status(HttpStatusCode.BadGateway));
        using var services = BuildServices(network);

        var result = await services.GetRequiredService<IThreatProvider<SafeBrowsingEvidence>>()
            .CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Equal(3, network.Requests.Count); // first attempt + 2 retries
    }

    [Fact]
    public async Task A_hanging_source_times_out_into_unavailable_instead_of_blocking_the_scan()
    {
        var network = new HangingHandler();
        using var services = BuildServices(network);
        using var guard = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var result = await services.GetRequiredService<IThreatProvider<VirusTotalEvidence>>()
            .CheckAsync(Target, guard.Token);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Equal(3, network.Calls);
    }

    [Fact]
    public async Task Client_errors_such_as_404_are_not_retried()
    {
        var network = new StubHttpHandler(_ => StubHttpHandler.Status(HttpStatusCode.NotFound));
        using var services = BuildServices(network);

        await services.GetRequiredService<IThreatProvider<RdapEvidence>>().CheckAsync(new Uri("https://example.com/"), CancellationToken.None);

        Assert.Single(network.Requests); // "example.com" has two labels, so one lookup, one request
    }
}
