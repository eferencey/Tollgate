// One explainable reason a score went down: what a source reported, the points it cost,
// and (for severe findings) the highest score the URL may still have.
namespace Tollgate.Domain;

/// <param name="Source">The source that reported the problem.</param>
/// <param name="Kind">Stable code for the type of problem; clients key their own explanations on it, never on the description.</param>
/// <param name="Description">Plain-English statement of what was found.</param>
/// <param name="PointsDeducted">Positive number of points subtracted from 100.</param>
/// <param name="ScoreCap">Upper bound on the final score caused by this finding, if any.</param>
public sealed record Finding(
    ThreatSource Source,
    FindingKind Kind,
    string Description,
    int PointsDeducted,
    int? ScoreCap = null);
