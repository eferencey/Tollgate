// Everything the API needs to answer one scan: the explainable score plus the per-source state.
using Tollgate.Domain;

namespace Tollgate.Application;

/// <param name="Result">Score computed from whatever evidence is available right now.</param>
/// <param name="Sources">One entry per source, in <see cref="ThreatSource"/> order.</param>
/// <param name="FromCache">True when no source had to be queried (everything came from the cache).</param>
/// <param name="ScannedAt">When the first evidence for this URL was gathered.</param>
public sealed record ScanOutcome(
    ScanResult Result,
    IReadOnlyList<SourceStatus> Sources,
    bool FromCache,
    DateTimeOffset ScannedAt)
{
    /// <summary>False while any source is still analysing: the score is provisional and the client should poll.</summary>
    public bool IsComplete => Sources.All(s => s.State != SourceState.Pending);
}
