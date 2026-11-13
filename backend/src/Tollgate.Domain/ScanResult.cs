// The complete, explainable outcome of scanning one URL.
namespace Tollgate.Domain;

/// <param name="Url">The normalized URL that was scanned.</param>
/// <param name="Score">Trust score, 0 (dangerous) to 100 (clean).</param>
/// <param name="Findings">Every deduction, each traced to its source.</param>
/// <param name="CappedBy">
/// The finding whose cap lowered the score below "100 minus penalties";
/// null when no cap was binding.
/// </param>
/// <param name="UnavailableSources">Sources that did not respond; they lower the confidence.</param>
public sealed record ScanResult(
    string Url,
    int Score,
    Verdict Verdict,
    Confidence Confidence,
    IReadOnlyList<Finding> Findings,
    Finding? CappedBy,
    IReadOnlyList<ThreatSource> UnavailableSources);
