// URLhaus: the exact URL's malware status (severe, capped) plus the host's malware history (capped penalty).
// The two findings are independent rows of the scoring table and therefore add up.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Rules;

public sealed class UrlHausRule(UrlHausScoring options) : IScoringRule
{
    public IEnumerable<Finding> Evaluate(ScanEvidence evidence)
    {
        if (evidence.UrlHaus is not { } urlHaus)
        {
            yield break;
        }

        switch (urlHaus.UrlStatus)
        {
            case UrlHausUrlStatus.ActivelyDistributing:
                yield return new Finding(
                    ThreatSource.UrlHaus,
                    FindingKind.UrlActivelyDistributingMalware,
                    "URL is actively distributing malware",
                    options.ActiveUrlPenalty,
                    options.ActiveUrlCap);
                break;
            case UrlHausUrlStatus.PastRecord:
                yield return new Finding(
                    ThreatSource.UrlHaus,
                    FindingKind.UrlDistributedMalwareInPast,
                    "URL distributed malware in the past",
                    options.PastUrlPenalty,
                    options.PastUrlCap);
                break;
        }

        if (urlHaus.HostRecordCount > 0)
        {
            var penalty = options.HostRecordBasePenalty + options.HostRecordPenaltyPerRecord * urlHaus.HostRecordCount;
            yield return new Finding(
                ThreatSource.UrlHaus,
                FindingKind.DomainHasMalwareRecords,
                $"Past malware records on the domain: {urlHaus.HostRecordCount}",
                Math.Min(penalty, options.HostRecordMaxPenalty));
        }
    }
}
