// Real sites have a name; a link that points straight at a numeric address (often a hacked or throwaway
// server) is a common phishing and malware-delivery shape. Decimal forms such as http://3232235777/ count too,
// because Uri parses them as addresses.
using Tollgate.Domain;

namespace Tollgate.Scoring.Heuristics;

public sealed class IpHostHeuristic(PenaltyScoring options) : IUrlHeuristic
{
    public IEnumerable<Finding> Evaluate(Uri url)
    {
        if (url.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            yield return new Finding(ThreatSource.LocalAnalysis, FindingKind.RawIpAddressHost, $"Link points at a raw IP address ({url.IdnHost}) instead of a domain name", options.Penalty);
        }
    }
}
