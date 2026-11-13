// What RDAP reports about the domain registration.
namespace Tollgate.Domain.Evidence;

/// <param name="DomainAgeDays">
/// Days since registration; null when RDAP answered but the registry
/// (some country TLDs) does not publish a registration date.
/// </param>
public sealed record RdapEvidence(int? DomainAgeDays);
