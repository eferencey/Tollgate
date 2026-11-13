// Shared error isolation for all providers (template method): subclasses write the happy path,
// this class guarantees that any failure becomes "Unavailable" instead of failing the whole scan.
using Microsoft.Extensions.Logging;
using Tollgate.Domain;

namespace Tollgate.Providers;

public abstract class ThreatProviderBase<TEvidence>(ILogger logger) : IThreatProvider<TEvidence>
    where TEvidence : class
{
    public abstract ThreatSource Source { get; }

    public async Task<ProviderResult<TEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            return await QueryAsync(url, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller gave up; that is not this source's failure.
            throw;
        }
        catch (Exception ex)
        {
            // Deliberately broad: this is the isolation boundary. Timeouts (Polly's TimeoutRejectedException),
            // network errors, non-success HTTP codes and malformed JSON all end up here.
            logger.LogWarning(ex, "{Source} provider failed; continuing without it", Source);
            return ProviderResult<TEvidence>.Unavailable($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    protected abstract Task<ProviderResult<TEvidence>> QueryAsync(Uri url, CancellationToken cancellationToken);

    protected ProviderResult<TEvidence> MissingApiKey() =>
        ProviderResult<TEvidence>.Unavailable("API key not configured");
}
