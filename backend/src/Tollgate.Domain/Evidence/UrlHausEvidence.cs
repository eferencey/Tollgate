// What URLhaus knows: the status of the exact URL, and how many malware records exist for its host.
namespace Tollgate.Domain.Evidence;

public sealed record UrlHausEvidence(UrlHausUrlStatus UrlStatus, int HostRecordCount);
