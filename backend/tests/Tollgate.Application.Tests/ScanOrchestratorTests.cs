// The orchestrator against scripted providers: parallelism, error isolation, Pending vs Unavailable and the cache.
using Microsoft.Extensions.Logging.Abstractions;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;
using Tollgate.Providers;
using Tollgate.Scoring;

namespace Tollgate.Application.Tests;

public class ScanOrchestratorTests
{
    private const string Url = "https://example.com/";
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    // Only the numbers these tests touch matter; the real model is covered by Tollgate.Scoring.Tests.
    private static readonly ScoringOptions Scoring = new()
    {
        VirusTotal = new()
        {
            MaliciousPenaltyPerEngine = 12,
            MaliciousMaxPenalty = 60,
            SuspiciousPenaltyPerEngine = 5,
            SuspiciousMaxPenalty = 20,
            MaliciousCaps = [],
        },
        SafeBrowsing = new() { ListedPenalty = 60, ListedCap = 20 },
        UrlHaus = new()
        {
            ActiveUrlPenalty = 80,
            ActiveUrlCap = 5,
            PastUrlPenalty = 40,
            PastUrlCap = 35,
            HostRecordBasePenalty = 8,
            HostRecordPenaltyPerRecord = 2,
            HostRecordMaxPenalty = 30,
        },
        DomainAge = new() { VeryNewBelowDays = 30, VeryNewPenalty = 15, NewBelowDays = 180, NewPenalty = 5 },
        LocalAnalysis = new()
        {
            Lookalike = new() { NonLatinAlphabetPenalty = 5, MixedAlphabetPenalty = 40 },
            Typosquatting = new()
            {
                ProtectedDomains = ["paypal.com"],
                NearMatchMinLength = 5,
                MaxEditDistance = 1,
                SubstitutionPenalty = 45,
                NearMatchPenalty = 30,
            },
            SuspiciousTld = new() { Tlds = ["top"], Penalty = 8 },
            IpHost = new() { Penalty = 15 },
            UserInfo = new() { Penalty = 25 },
        },
        VerdictThresholds = new() { DangerousBelow = 50, SuspiciousBelow = 80 },
        ConfidenceThresholds = new() { HighMinSources = 4, MediumMinSources = 2 },
    };

    private sealed class Rig
    {
        public ScriptedProvider<VirusTotalEvidence> VirusTotal { get; init; } = Completed(ThreatSource.VirusTotal, new VirusTotalEvidence(0, 0));
        public ScriptedProvider<SafeBrowsingEvidence> SafeBrowsing { get; init; } = Completed(ThreatSource.SafeBrowsing, new SafeBrowsingEvidence(false));
        public ScriptedProvider<UrlHausEvidence> UrlHaus { get; init; } = Completed(ThreatSource.UrlHaus, new UrlHausEvidence(UrlHausUrlStatus.NotListed, 0));
        public ScriptedProvider<RdapEvidence> Rdap { get; init; } = Completed(ThreatSource.Rdap, new RdapEvidence(3650));
        public DictionaryCache Cache { get; } = new();
        public IEvidenceCache? CacheOverride { get; init; }
        public ListHistory History { get; init; } = new();
        public FakeClock Clock { get; } = new(Start);

        public ScanOrchestrator Orchestrator => new(
            VirusTotal, SafeBrowsing, UrlHaus, Rdap,
            new ScoringEngine(Scoring), CacheOverride ?? Cache, History, Clock, NullLogger<ScanOrchestrator>.Instance);
    }

    private static ScriptedProvider<T> Completed<T>(ThreatSource source, T evidence) where T : class =>
        new(source, _ => ProviderResult<T>.Completed(evidence));

    private static ScriptedProvider<T> Failing<T>(ThreatSource source, string reason) where T : class =>
        new(source, _ => ProviderResult<T>.Unavailable(reason));

    private static ScriptedProvider<T> Pending<T>(ThreatSource source) where T : class =>
        new(source, _ => ProviderResult<T>.Pending());

    [Fact]
    public async Task Evidence_from_all_four_sources_is_scored_with_full_confidence()
    {
        var rig = new Rig { VirusTotal = Completed(ThreatSource.VirusTotal, new VirusTotalEvidence(MaliciousEngines: 1, SuspiciousEngines: 0)) };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(88, outcome.Result.Score);
        Assert.Equal(Confidence.High, outcome.Result.Confidence);
        Assert.All(outcome.Sources, s => Assert.Equal(SourceState.Completed, s.State));
        Assert.Equal(
            [ThreatSource.VirusTotal, ThreatSource.SafeBrowsing, ThreatSource.UrlHaus, ThreatSource.Rdap],
            outcome.Sources.Select(s => s.Source));
        Assert.True(outcome.IsComplete);
        Assert.False(outcome.FromCache);
        Assert.Equal(Start, outcome.ScannedAt);
    }

    [Fact]
    public async Task All_four_sources_are_queried_at_the_same_time()
    {
        // Each provider waits until all four have started. Sequential code would never get past the first one.
        var allStarted = new TaskCompletionSource();
        var started = 0;
        Task Rendezvous()
        {
            if (Interlocked.Increment(ref started) == 4)
            {
                allStarted.SetResult();
            }

            return allStarted.Task;
        }

        var rig = new Rig
        {
            VirusTotal = new(ThreatSource.VirusTotal, _ => ProviderResult<VirusTotalEvidence>.Completed(new(0, 0))) { BeforeAnswer = Rendezvous },
            SafeBrowsing = new(ThreatSource.SafeBrowsing, _ => ProviderResult<SafeBrowsingEvidence>.Completed(new(false))) { BeforeAnswer = Rendezvous },
            UrlHaus = new(ThreatSource.UrlHaus, _ => ProviderResult<UrlHausEvidence>.Completed(new(UrlHausUrlStatus.NotListed, 0))) { BeforeAnswer = Rendezvous },
            Rdap = new(ThreatSource.Rdap, _ => ProviderResult<RdapEvidence>.Completed(new(3650))) { BeforeAnswer = Rendezvous },
        };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(outcome.IsComplete);
    }

    [Fact]
    public async Task An_unavailable_source_does_not_fail_the_scan_and_lowers_confidence()
    {
        var rig = new Rig { SafeBrowsing = Failing<SafeBrowsingEvidence>(ThreatSource.SafeBrowsing, "timeout") };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        var gsb = Assert.Single(outcome.Sources, s => s.Source == ThreatSource.SafeBrowsing);
        Assert.Equal(SourceState.Unavailable, gsb.State);
        Assert.Equal("timeout", gsb.Reason);
        Assert.Equal(Confidence.Medium, outcome.Result.Confidence);
        Assert.True(outcome.IsComplete); // unavailable is final for this scan; only pending asks for a re-poll
    }

    [Fact]
    public async Task Pending_is_reported_separately_from_unavailable_and_marks_the_scan_incomplete()
    {
        var rig = new Rig
        {
            VirusTotal = Pending<VirusTotalEvidence>(ThreatSource.VirusTotal),
            Rdap = Failing<RdapEvidence>(ThreatSource.Rdap, "down"),
        };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(SourceState.Pending, outcome.Sources.Single(s => s.Source == ThreatSource.VirusTotal).State);
        Assert.Equal(SourceState.Unavailable, outcome.Sources.Single(s => s.Source == ThreatSource.Rdap).State);
        Assert.False(outcome.IsComplete);
        Assert.Equal(Confidence.Medium, outcome.Result.Confidence);
    }

    [Fact]
    public async Task A_provider_that_breaks_its_contract_by_throwing_is_isolated()
    {
        var rig = new Rig
        {
            UrlHaus = new(ThreatSource.UrlHaus, _ => throw new InvalidOperationException("bug in adapter")),
        };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(SourceState.Unavailable, outcome.Sources.Single(s => s.Source == ThreatSource.UrlHaus).State);
        Assert.Equal(3, outcome.Sources.Count(s => s.State == SourceState.Completed));
    }

    [Fact]
    public async Task Caller_cancellation_propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var rig = new Rig
        {
            Rdap = new(ThreatSource.Rdap, _ => throw new OperationCanceledException(cts.Token)),
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.Orchestrator.ScanAsync(Url, cts.Token));
    }

    [Fact]
    public async Task A_repeated_scan_is_served_entirely_from_the_cache()
    {
        var rig = new Rig();

        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);
        var second = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.True(second.FromCache);
        Assert.Equal(1, rig.VirusTotal.Calls);
        Assert.Equal(1, rig.SafeBrowsing.Calls);
        Assert.Equal(1, rig.UrlHaus.Calls);
        Assert.Equal(1, rig.Rdap.Calls);
    }

    [Fact]
    public async Task A_repoll_asks_only_the_source_that_was_pending_and_keeps_the_original_scan_time()
    {
        var vtAnswer = ProviderResult<VirusTotalEvidence>.Pending();
        var rig = new Rig { VirusTotal = new(ThreatSource.VirusTotal, _ => vtAnswer) };

        var first = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);
        Assert.False(first.IsComplete);

        vtAnswer = ProviderResult<VirusTotalEvidence>.Completed(new(MaliciousEngines: 2, SuspiciousEngines: 0));
        rig.Clock.Now = Start.AddSeconds(20);
        var second = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.True(second.IsComplete);
        Assert.False(second.FromCache);
        Assert.Equal(76, second.Result.Score);
        Assert.Equal(2, rig.VirusTotal.Calls);
        Assert.Equal(1, rig.SafeBrowsing.Calls);
        Assert.Equal(1, rig.UrlHaus.Calls);
        Assert.Equal(1, rig.Rdap.Calls);
        Assert.Equal(Start, second.ScannedAt); // a late answer must not extend the cache lifetime
    }

    [Fact]
    public async Task Unavailable_answers_are_not_remembered_so_the_next_scan_retries_them()
    {
        var healthy = false;
        var rig = new Rig
        {
            Rdap = new(ThreatSource.Rdap, _ => healthy
                ? ProviderResult<RdapEvidence>.Completed(new(3650))
                : ProviderResult<RdapEvidence>.Unavailable("down")),
        };

        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);
        healthy = true;
        var second = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(SourceState.Completed, second.Sources.Single(s => s.Source == ThreatSource.Rdap).State);
        Assert.Equal(2, rig.Rdap.Calls);
    }

    [Fact]
    public async Task A_scan_where_nothing_answered_stores_nothing()
    {
        var rig = new Rig
        {
            VirusTotal = Failing<VirusTotalEvidence>(ThreatSource.VirusTotal, "x"),
            SafeBrowsing = Failing<SafeBrowsingEvidence>(ThreatSource.SafeBrowsing, "x"),
            UrlHaus = Failing<UrlHausEvidence>(ThreatSource.UrlHaus, "x"),
            Rdap = Failing<RdapEvidence>(ThreatSource.Rdap, "x"),
        };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(0, rig.Cache.Count);
        Assert.Equal(Confidence.Low, outcome.Result.Confidence);
        Assert.Empty(rig.History.Entries); // "no data" is not a Safe scan and must not skew the statistics
    }

    [Fact]
    public async Task Local_findings_do_not_count_as_a_source_answering()
    {
        var rig = new Rig
        {
            VirusTotal = Failing<VirusTotalEvidence>(ThreatSource.VirusTotal, "x"),
            SafeBrowsing = Failing<SafeBrowsingEvidence>(ThreatSource.SafeBrowsing, "x"),
            UrlHaus = Failing<UrlHausEvidence>(ThreatSource.UrlHaus, "x"),
            Rdap = Failing<RdapEvidence>(ThreatSource.Rdap, "x"),
        };

        var outcome = await rig.Orchestrator.ScanAsync("https://paypa1.com/", CancellationToken.None);

        // The local check did find something, but "no data" must stay "no data": nothing cached, nothing in the history.
        Assert.Equal(ThreatSource.LocalAnalysis, Assert.Single(outcome.Result.Findings).Source);
        Assert.Equal(Confidence.Low, outcome.Result.Confidence);
        Assert.All(outcome.Sources, s => Assert.Equal(SourceState.Unavailable, s.State));
        Assert.Equal(4, outcome.Sources.Count);
        Assert.Equal(0, rig.Cache.Count);
        Assert.Empty(rig.History.Entries);
    }

    [Fact]
    public async Task A_finished_scan_is_recorded_in_the_history_with_the_full_url_and_host()
    {
        var rig = new Rig { VirusTotal = Completed(ThreatSource.VirusTotal, new VirusTotalEvidence(1, 0)) };
        rig.Clock.Now = Start.AddMinutes(5);

        await rig.Orchestrator.ScanAsync("https://shop.example.com/cart?id=7", CancellationToken.None);

        var entry = Assert.Single(rig.History.Entries);
        Assert.Equal("https://shop.example.com/cart?id=7", entry.Url);
        Assert.Equal("shop.example.com", entry.Host);
        Assert.Equal(88, entry.Score);
        Assert.Equal(Verdict.Safe, entry.Verdict);
        Assert.Equal(Confidence.High, entry.Confidence);
        Assert.Equal(Start.AddMinutes(5), entry.ScannedAt);
    }

    [Fact]
    public async Task A_provisional_scan_is_not_recorded_until_the_pending_source_has_answered()
    {
        var vtAnswer = ProviderResult<VirusTotalEvidence>.Pending();
        var rig = new Rig { VirusTotal = new(ThreatSource.VirusTotal, _ => vtAnswer) };

        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);
        Assert.Empty(rig.History.Entries);

        vtAnswer = ProviderResult<VirusTotalEvidence>.Completed(new(0, 0));
        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Single(rig.History.Entries);
    }

    [Fact]
    public async Task A_repeat_served_entirely_from_the_cache_is_not_recorded_again()
    {
        var rig = new Rig();

        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);
        await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Single(rig.History.Entries);
    }

    [Fact]
    public async Task A_broken_cache_does_not_fail_the_scan()
    {
        var rig = new Rig { CacheOverride = new ThrowingCache() };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(100, outcome.Result.Score);
        Assert.Equal(1, rig.VirusTotal.Calls); // nothing could be reused, so every source was asked
        Assert.Single(rig.History.Entries);
    }

    [Fact]
    public async Task A_broken_history_does_not_fail_the_scan_and_the_evidence_is_still_cached()
    {
        var rig = new Rig { History = new ListHistory { Failure = new InvalidOperationException("db locked") } };

        var outcome = await rig.Orchestrator.ScanAsync(Url, CancellationToken.None);

        Assert.Equal(100, outcome.Result.Score);
        Assert.Equal(1, rig.Cache.Count);
    }
}
