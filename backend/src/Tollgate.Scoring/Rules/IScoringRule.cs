// Strategy interface: one rule turns one source's evidence into zero or more findings.
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Scoring.Rules;

public interface IScoringRule
{
    IEnumerable<Finding> Evaluate(ScanEvidence evidence);
}
