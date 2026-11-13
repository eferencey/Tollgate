// Google Safe Browsing: a blacklist match is a severe finding with a hard score ceiling.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Rules;

public sealed class SafeBrowsingRule(SafeBrowsingScoring options) : IScoringRule
{
    public IEnumerable<Finding> Evaluate(ScanEvidence evidence)
    {
        if (evidence.SafeBrowsing is { IsListed: true })
        {
            yield return new Finding(
                ThreatSource.SafeBrowsing,
                FindingKind.SafeBrowsingListed,
                "URL is on a Google Safe Browsing blacklist",
                options.ListedPenalty,
                options.ListedCap);
        }
    }
}
