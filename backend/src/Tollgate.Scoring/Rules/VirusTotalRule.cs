// VirusTotal: penalize each malicious/suspicious engine (with a ceiling on the total)
// and cap the score once several engines agree the URL is malicious.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Rules;

public sealed class VirusTotalRule(VirusTotalScoring options) : IScoringRule
{
    public IEnumerable<Finding> Evaluate(ScanEvidence evidence)
    {
        if (evidence.VirusTotal is not { } virusTotal)
        {
            yield break;
        }

        var malicious = virusTotal.MaliciousEngines;
        if (malicious > 0)
        {
            yield return new Finding(
                ThreatSource.VirusTotal,
                FindingKind.EnginesMalicious,
                $"Engines flagging the URL as malicious: {malicious}",
                Math.Min(malicious * options.MaliciousPenaltyPerEngine, options.MaliciousMaxPenalty),
                LowestMatchingCap(malicious));
        }

        var suspicious = virusTotal.SuspiciousEngines;
        if (suspicious > 0)
        {
            yield return new Finding(
                ThreatSource.VirusTotal,
                FindingKind.EnginesSuspicious,
                $"Engines flagging the URL as suspicious: {suspicious}",
                Math.Min(suspicious * options.SuspiciousPenaltyPerEngine, options.SuspiciousMaxPenalty));
        }
    }

    private int? LowestMatchingCap(int maliciousEngines) =>
        options.MaliciousCaps
            .Where(cap => maliciousEngines >= cap.MinEngines)
            .Select(cap => (int?)cap.Cap)
            .Min();
}
