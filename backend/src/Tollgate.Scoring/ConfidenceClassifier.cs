// Maps "how many main sources responded" to a confidence level.
using Tollgate.Domain;

namespace Tollgate.Scoring;

public sealed class ConfidenceClassifier(ConfidenceThresholds thresholds)
{
    public Confidence Classify(int respondedSources)
    {
        if (respondedSources >= thresholds.HighMinSources)
        {
            return Confidence.High;
        }

        return respondedSources >= thresholds.MediumMinSources ? Confidence.Medium : Confidence.Low;
    }
}
