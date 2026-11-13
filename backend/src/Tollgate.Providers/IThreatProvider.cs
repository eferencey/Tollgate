// The contract every threat-intelligence source implements: one URL in, one typed piece of evidence out.
// Adding a source = one new class implementing this + one registration line.
using Tollgate.Domain;

namespace Tollgate.Providers;

/// <typeparam name="TEvidence">The evidence record this source produces (one of the Domain.Evidence types).</typeparam>
public interface IThreatProvider<TEvidence> where TEvidence : class
{
    ThreatSource Source { get; }

    /// <summary>
    /// Asks the source about a normalized URL. Never throws because of a source failure
    /// (that becomes <see cref="ProviderStatus.Unavailable"/>); only the caller's own cancellation propagates.
    /// </summary>
    Task<ProviderResult<TEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken);
}
