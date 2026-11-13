// How URLhaus classifies the exact URL being scanned.
namespace Tollgate.Domain.Evidence;

public enum UrlHausUrlStatus
{
    NotListed,
    PastRecord,
    ActivelyDistributing,
}
