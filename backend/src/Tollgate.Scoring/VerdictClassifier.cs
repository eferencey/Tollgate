// Maps a 0-100 score to a verdict. Lower bounds are inclusive: exactly 50 is Suspicious, exactly 80 is Safe.
using Tollgate.Domain;

namespace Tollgate.Scoring;

public sealed class VerdictClassifier(VerdictThresholds thresholds)
{
    public Verdict Classify(int score)
    {
        if (score < thresholds.DangerousBelow)
        {
            return Verdict.Dangerous;
        }

        return score < thresholds.SuspiciousBelow ? Verdict.Suspicious : Verdict.Safe;
    }
}
