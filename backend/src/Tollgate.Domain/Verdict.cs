// The human-readable decision derived from a 0-100 trust score.
namespace Tollgate.Domain;

public enum Verdict
{
    Dangerous,
    Suspicious,
    Safe,
}
