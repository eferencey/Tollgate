// Where a finding comes from. The first four are the threat-intelligence sources a scan queries over the
// network ("confidence" counts how many of these responded). LocalAnalysis is our own check of the URL's
// shape: it needs no network, always "answers", and so is not one of the queried sources (see ScanEvidence).
namespace Tollgate.Domain;

public enum ThreatSource
{
    VirusTotal,
    SafeBrowsing,
    UrlHaus,
    Rdap,
    LocalAnalysis,
}
