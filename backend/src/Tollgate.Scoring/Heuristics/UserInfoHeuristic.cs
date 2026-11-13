// The "@" trick: in "https://google.com@evil.example/" everything before the @ is a username that the
// server ignores, so the real site is evil.example. Almost no legitimate web link contains a username.
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public sealed class UserInfoHeuristic(PenaltyScoring options) : IUrlHeuristic
{
    public IEnumerable<Finding> Evaluate(Uri url)
    {
        if (!string.IsNullOrEmpty(url.UserInfo))
        {
            yield return new Finding(
                ThreatSource.LocalAnalysis,
                FindingKind.HiddenBehindUserInfo,
                $"Link hides its real site behind a username part (\"@\"): the page is actually on {url.IdnHost}",
                options.Penalty);
        }
    }
}
