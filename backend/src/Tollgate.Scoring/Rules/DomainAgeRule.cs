// RDAP: freshly registered domains are a classic phishing marker. Only one age tier applies.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Rules;

public sealed class DomainAgeRule(DomainAgeScoring options) : IScoringRule
{
    public IEnumerable<Finding> Evaluate(ScanEvidence evidence)
    {
        if (evidence.Rdap?.DomainAgeDays is not { } ageDays)
        {
            yield break;
        }

        if (ageDays < options.VeryNewBelowDays)
        {
            yield return DomainTooNew(ageDays, options.VeryNewPenalty);
        }
        else if (ageDays < options.NewBelowDays)
        {
            yield return DomainTooNew(ageDays, options.NewPenalty);
        }
    }

    private static Finding DomainTooNew(int ageDays, int penalty) =>
        new(ThreatSource.Rdap, FindingKind.DomainRecentlyRegistered, $"Domain was registered only {ageDays} days ago", penalty);
}
