// Threshold boundaries for verdicts (50 -> Suspicious, 80 -> Safe) and confidence levels.
using Tollgate.Domain;

namespace Tollgate.Scoring.Tests;

public class ClassifierTests
{
    private readonly VerdictClassifier _verdicts = new(ScoringTestConfig.Options.VerdictThresholds);
    private readonly ConfidenceClassifier _confidence = new(ScoringTestConfig.Options.ConfidenceThresholds);

    [Theory]
    [InlineData(0, Verdict.Dangerous)]
    [InlineData(49, Verdict.Dangerous)]
    [InlineData(50, Verdict.Suspicious)] // boundary belongs to the higher band
    [InlineData(79, Verdict.Suspicious)]
    [InlineData(80, Verdict.Safe)] // boundary belongs to the higher band
    [InlineData(100, Verdict.Safe)]
    public void Verdict_boundaries_include_the_lower_bound(int score, Verdict expected)
    {
        Assert.Equal(expected, _verdicts.Classify(score));
    }

    [Theory]
    [InlineData(4, Confidence.High)]
    [InlineData(3, Confidence.Medium)]
    [InlineData(2, Confidence.Medium)]
    [InlineData(1, Confidence.Low)]
    [InlineData(0, Confidence.Low)]
    public void Confidence_depends_on_how_many_sources_responded(int responded, Confidence expected)
    {
        Assert.Equal(expected, _confidence.Classify(responded));
    }
}
