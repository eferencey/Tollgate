// Whether Google Safe Browsing lists the URL as phishing/malware/unwanted software.
namespace Tollgate.Domain.Evidence;

public sealed record SafeBrowsingEvidence(bool IsListed);
