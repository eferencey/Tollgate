// ValidateOnStart: the shipped appsettings.json must be valid, and a bad setting must stop the app at startup
// with a message that names the setting, instead of surfacing as wrong scores or a crash on the first request.
using Microsoft.Extensions.Options;

namespace Tollgate.Api.Tests;

public class StartupValidationTests
{
    private static string[] StartupFailureMessages(TollgateFactory factory)
    {
        using (factory)
        {
            var failure = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
            var validation = Flatten(failure).OfType<OptionsValidationException>().FirstOrDefault();
            Assert.True(validation is not null, $"expected an OptionsValidationException but got: {failure}");
            return validation.Failures.ToArray();
        }
    }

    private static IEnumerable<Exception> Flatten(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.SelectMany(Flatten))
                {
                    yield return inner;
                }
            }
        }
    }

    [Fact]
    public void The_shipped_settings_are_valid_in_demo_mode()
    {
        using var factory = new TollgateFactory();

        using var client = factory.CreateClient();
    }

    [Fact]
    public void The_shipped_settings_are_valid_in_live_mode_including_provider_and_resilience_settings()
    {
        using var factory = new TollgateFactory().With("GSB_API_KEY", "test-key");

        using var client = factory.CreateClient();
    }

    [Fact]
    public void Inverted_verdict_thresholds_stop_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scoring:VerdictThresholds:DangerousBelow", "90"));

        Assert.Contains(failures, f => f.Contains("SuspiciousBelow must be greater than DangerousBelow"));
    }

    [Fact]
    public void A_negative_penalty_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scoring:SafeBrowsing:ListedPenalty", "-60"));

        Assert.Contains(failures, f => f.Contains("SafeBrowsing:ListedPenalty must be between 0 and 100"));
    }

    [Fact]
    public void A_negative_local_analysis_penalty_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scoring:LocalAnalysis:IpHost:Penalty", "-1"));

        Assert.Contains(failures, f => f.Contains("LocalAnalysis:IpHost:Penalty must be between 0 and 100"));
    }

    [Fact]
    public void A_protected_domain_that_could_never_match_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scoring:LocalAnalysis:Typosquatting:ProtectedDomains:0", "PayPal"));

        Assert.Contains(failures, f => f.Contains("ProtectedDomains entry \"PayPal\""));
    }

    [Fact]
    public void A_top_level_domain_written_with_a_dot_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scoring:LocalAnalysis:SuspiciousTld:Tlds:0", ".top"));

        Assert.Contains(failures, f => f.Contains("Tlds entry \".top\""));
    }

    [Fact]
    public void A_zero_cache_duration_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Scan:CacheDuration", "00:00:00"));

        Assert.Contains(failures, f => f.Contains("CacheDuration"));
    }

    [Fact]
    public void A_zero_daily_quota_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("VirusTotalQueue:DailyLimit", "0"));

        Assert.Contains(failures, f => f.Contains("DailyLimit"));
    }

    [Fact]
    public void A_zero_rate_limit_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("RateLimiting:PermitLimit", "0"));

        Assert.Contains(failures, f => f.Contains("PermitLimit"));
    }

    [Fact]
    public void A_zero_retention_period_stops_the_app()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("History:RetentionDays", "0"));

        Assert.Contains(failures, f => f.Contains("RetentionDays"));
    }

    [Fact]
    public void A_missing_data_directory_stops_the_app_before_it_writes_a_database_somewhere_unexpected()
    {
        var failures = StartupFailureMessages(new TollgateFactory().With("Persistence:DataDirectory", ""));

        Assert.Contains(failures, f => f.Contains("DataDirectory"));
    }

    [Fact]
    public void A_provider_address_without_a_trailing_slash_stops_the_live_app()
    {
        var failures = StartupFailureMessages(
            new TollgateFactory().With("GSB_API_KEY", "test-key").With("Providers:Rdap:BaseAddress", "https://rdap.example/v2"));

        Assert.Contains(failures, f => f.Contains("Providers:Rdap:BaseAddress must end with a slash"));
    }

    [Fact]
    public void Invalid_resilience_numbers_stop_the_live_app_at_startup_not_at_the_first_request()
    {
        var failures = StartupFailureMessages(
            new TollgateFactory().With("GSB_API_KEY", "test-key").With("Providers:Resilience:Retry:MaxRetryAttempts", "0"));

        Assert.Contains(failures, f => f.Contains("MaxRetryAttempts"));
    }
}
