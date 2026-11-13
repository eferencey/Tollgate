// The JSON body of GET /api/scan: one flat envelope with the score, every deduction and the state of each source.
using Tollgate.Application;
using Tollgate.Domain;

namespace Tollgate.Api.Contracts;

/// <summary>"Complete": no source is still analysing. "Partial": the score is provisional, ask again after RetryAfterSeconds.</summary>
public enum ScanStatus
{
    Complete,
    Partial,
}

/// <param name="Mode">"Demo" when no API keys are configured and the sources are canned examples.</param>
/// <param name="Url">The normalized URL that was scanned (the cache key).</param>
/// <param name="Findings">Every point deduction, each traced to its source.</param>
/// <param name="CappedBy">The finding whose cap lowered the score; absent when no cap applied.</param>
/// <param name="Sources">State of each of the four sources: completed, pending (still analysing) or unavailable (with reason).</param>
/// <param name="FromCache">True when the answer needed no source call.</param>
/// <param name="ScannedAt">When evidence for this URL was first gathered.</param>
/// <param name="RetryAfterSeconds">Present only for a partial result: how long to wait before asking again.</param>
public sealed record ScanResponse(
    ScanStatus Status,
    ScanMode Mode,
    string Url,
    int Score,
    Verdict Verdict,
    Confidence Confidence,
    IReadOnlyList<Finding> Findings,
    Finding? CappedBy,
    IReadOnlyList<SourceStatus> Sources,
    bool FromCache,
    DateTimeOffset ScannedAt,
    int? RetryAfterSeconds)
{
    public static ScanResponse From(ScanOutcome outcome, ScanMode mode, TimeSpan pollInterval) => new(
        outcome.IsComplete ? ScanStatus.Complete : ScanStatus.Partial,
        mode,
        outcome.Result.Url,
        outcome.Result.Score,
        outcome.Result.Verdict,
        outcome.Result.Confidence,
        outcome.Result.Findings,
        outcome.Result.CappedBy,
        outcome.Sources,
        outcome.FromCache,
        outcome.ScannedAt,
        outcome.IsComplete ? null : (int)pollInterval.TotalSeconds);
}
