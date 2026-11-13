// Some top-level domains (.zip, .top, .click ...) are cheap or free to register and so are used for
// abuse far more than for real sites. The list is configuration, not code, because it ages quickly.
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public sealed class SuspiciousTldHeuristic(SuspiciousTldScoring options) : IUrlHeuristic
{
    private readonly HashSet<string> _tlds = options.Tlds.ToHashSet(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Finding> Evaluate(Uri url)
    {
        var labels = HostLabels.Of(url);

        // A single-label host ("localhost") has no top-level domain to judge.
        if (labels.Count < 2)
        {
            yield break;
        }

        var tld = labels[^1];
        if (_tlds.Contains(tld))
        {
            yield return new Finding(ThreatSource.LocalAnalysis, FindingKind.SuspiciousTld, $"Top-level domain \".{tld}\" is used for abuse far more often than average", options.Penalty);
        }
    }
}
