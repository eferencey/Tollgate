// What VirusTotal's antivirus engines concluded about a URL.
namespace Tollgate.Domain.Evidence;

public sealed record VirusTotalEvidence(int MaliciousEngines, int SuspiciousEngines);
