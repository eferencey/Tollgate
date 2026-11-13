// Strategy interface for local analysis: one check looks at the shape of the URL itself and yields
// zero or more findings. Pure (no I/O, no network), so it works the same in demo and live mode.
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public interface IUrlHeuristic
{
    IEnumerable<Finding> Evaluate(Uri url);
}
