// The score engine: score = min(100 - sum(penalties), lowest applicable cap). Pure, no I/O.
// The cap stops one severe finding from being "averaged away" by clean results from other sources.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;
using Tollgate.Scoring.Heuristics;
using Tollgate.Scoring.Rules;

namespace Tollgate.Scoring;

public sealed class ScoringEngine
{
    public const int MinScore = 0;
    public const int MaxScore = 100;

    private readonly IReadOnlyList<IScoringRule> _rules;
    private readonly IReadOnlyList<IUrlHeuristic> _heuristics;
    private readonly VerdictClassifier _verdictClassifier;
    private readonly ConfidenceClassifier _confidenceClassifier;

    public ScoringEngine(ScoringOptions options)
    {
        // Adding a new kind of evidence = one new rule, listed here.
        _rules =
        [
            new VirusTotalRule(options.VirusTotal),
            new SafeBrowsingRule(options.SafeBrowsing),
            new UrlHausRule(options.UrlHaus),
            new DomainAgeRule(options.DomainAge),
        ];

        // Local analysis reads the URL itself, so it works with or without any source answering.
        var local = options.LocalAnalysis;
        _heuristics =
        [
            new LookalikeAlphabetHeuristic(local.Lookalike),
            new TyposquattingHeuristic(local.Typosquatting),
            new SuspiciousTldHeuristic(local.SuspiciousTld),
            new IpHostHeuristic(local.IpHost),
            new UserInfoHeuristic(local.UserInfo),
        ];
        _verdictClassifier = new VerdictClassifier(options.VerdictThresholds);
        _confidenceClassifier = new ConfidenceClassifier(options.ConfidenceThresholds);
    }

    public ScanResult Score(string normalizedUrl, ScanEvidence evidence)
    {
        var url = new Uri(normalizedUrl);
        var findings = _rules.SelectMany(rule => rule.Evaluate(evidence))
            .Concat(_heuristics.SelectMany(heuristic => heuristic.Evaluate(url)))
            .ToList();

        var scoreBeforeCap = Math.Max(MinScore, MaxScore - findings.Sum(f => f.PointsDeducted));
        var lowestCap = findings
            .Where(f => f.ScoreCap is not null)
            .MinBy(f => f.ScoreCap);

        // A cap is reported only when it actually lowered the score, otherwise it would mislead the explanation.
        var capIsBinding = lowestCap is not null && lowestCap.ScoreCap < scoreBeforeCap;
        var score = capIsBinding ? lowestCap!.ScoreCap!.Value : scoreBeforeCap;

        return new ScanResult(
            normalizedUrl,
            score,
            _verdictClassifier.Classify(score),
            _confidenceClassifier.Classify(evidence.RespondedSources.Count),
            findings,
            capIsBinding ? lowestCap : null,
            evidence.UnavailableSources);
    }
}
