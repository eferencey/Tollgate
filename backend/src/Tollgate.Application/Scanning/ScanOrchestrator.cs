// Runs one scan (flow steps 2-5): reuse cached evidence, ask the missing sources in parallel,
// remember what answered, and let the scoring engine turn the evidence into an explainable result.
// Finished scans are also appended to the history.
using Microsoft.Extensions.Logging;
using Tollgate.Domain.Evidence;
using Tollgate.Providers;
using Tollgate.Scoring;

namespace Tollgate.Application;

public sealed class ScanOrchestrator(
    IThreatProvider<VirusTotalEvidence> virusTotal,
    IThreatProvider<SafeBrowsingEvidence> safeBrowsing,
    IThreatProvider<UrlHausEvidence> urlHaus,
    IThreatProvider<RdapEvidence> rdap,
    ScoringEngine scoring,
    IEvidenceCache cache,
    IScanHistory history,
    TimeProvider clock,
    ILogger<ScanOrchestrator> logger)
{
    /// <param name="normalizedUrl">Output of <c>UrlNormalizer</c>; it is also the cache key.</param>
    public async Task<ScanOutcome> ScanAsync(string normalizedUrl, CancellationToken cancellationToken)
    {
        var url = new Uri(normalizedUrl);

        CachedScan? cached = null;
        await BestEffortAsync("read the evidence cache", async () => cached = await cache.GetAsync(normalizedUrl, cancellationToken), cancellationToken);

        // Sources already in the cache are not asked again; a re-poll therefore only touches what is still missing.
        var vtTask = AskAsync(virusTotal, cached?.Evidence.VirusTotal, url, cancellationToken);
        var gsbTask = AskAsync(safeBrowsing, cached?.Evidence.SafeBrowsing, url, cancellationToken);
        var uhTask = AskAsync(urlHaus, cached?.Evidence.UrlHaus, url, cancellationToken);
        var rdapTask = AskAsync(rdap, cached?.Evidence.Rdap, url, cancellationToken);
        await Task.WhenAll(vtTask, gsbTask, uhTask, rdapTask);

        var (vt, gsb, uh, rd) = (vtTask.Result, gsbTask.Result, uhTask.Result, rdapTask.Result);
        var evidence = new ScanEvidence(vt.Evidence, gsb.Evidence, uh.Evidence, rd.Evidence);
        var statuses = new[] { vt.Status, gsb.Status, uh.Status, rd.Status };

        var fromCache = vt.FromCache && gsb.FromCache && uh.FromCache && rd.FromCache;
        var scannedAt = cached?.ScannedAt ?? clock.GetUtcNow();

        var outcome = new ScanOutcome(scoring.Score(normalizedUrl, evidence), statuses, fromCache, scannedAt);

        // A scan nobody answered scores 100 with Low confidence; that is "no data", so it is neither cached nor counted.
        if (fromCache || statuses.All(s => s.State != SourceState.Completed))
        {
            return outcome;
        }

        // Only answers are stored: Pending/Unavailable must be retried, not remembered.
        await BestEffortAsync("store the evidence", () => cache.SetAsync(normalizedUrl, new CachedScan(evidence, scannedAt), cancellationToken), cancellationToken);

        // One history entry per finished scan: provisional (Pending) scores and repeats served from the cache would skew the statistics.
        if (outcome.IsComplete)
        {
            var entry = new ScanHistoryEntry(
                0, normalizedUrl, url.IdnHost, outcome.Result.Score, outcome.Result.Verdict, outcome.Result.Confidence, clock.GetUtcNow());
            await BestEffortAsync("record the scan history", () => history.AddAsync(entry, cancellationToken), cancellationToken);
        }

        return outcome;
    }

    // The cache and the history are conveniences: a locked or full database must not turn a finished scan into an error.
    private async Task BestEffortAsync(string what, Func<Task> action, CancellationToken cancellationToken)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Could not {Action}; continuing without it", what);
        }
    }

    private async Task<SourceReport<TEvidence>> AskAsync<TEvidence>(
        IThreatProvider<TEvidence> provider, TEvidence? cachedEvidence, Uri url, CancellationToken cancellationToken)
        where TEvidence : class
    {
        if (cachedEvidence is not null)
        {
            return new(cachedEvidence, new SourceStatus(provider.Source, SourceState.Completed), FromCache: true);
        }

        try
        {
            var result = await provider.CheckAsync(url, cancellationToken);
            return result.Status switch
            {
                ProviderStatus.Completed => new(result.Evidence, new SourceStatus(provider.Source, SourceState.Completed), FromCache: false),
                ProviderStatus.Pending => new(null, new SourceStatus(provider.Source, SourceState.Pending), FromCache: false),
                _ => new(null, new SourceStatus(provider.Source, SourceState.Unavailable, result.Reason), FromCache: false),
            };
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Providers promise never to throw, but one buggy source must not take the other three down with it.
            logger.LogError(ex, "{Source} provider broke its contract by throwing", provider.Source);
            return new(null, new SourceStatus(provider.Source, SourceState.Unavailable, "Unexpected error"), FromCache: false);
        }
    }

    private sealed record SourceReport<TEvidence>(TEvidence? Evidence, SourceStatus Status, bool FromCache);
}
