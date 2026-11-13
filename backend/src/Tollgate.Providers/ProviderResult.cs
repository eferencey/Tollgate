// What a provider hands back: evidence, "still analysing", or "could not answer".
// Pending and Unavailable are kept apart so the UI can say "waiting for VirusTotal" instead of "VirusTotal is down".
namespace Tollgate.Providers;

public enum ProviderStatus
{
    /// <summary>The source answered; <see cref="ProviderResult{TEvidence}.Evidence"/> is set (even if it found nothing).</summary>
    Completed,

    /// <summary>The source accepted the URL but has no verdict yet; ask again later.</summary>
    Pending,

    /// <summary>Timeout, quota, missing key or any other failure. The scan continues without this source.</summary>
    Unavailable,
}

public sealed class ProviderResult<TEvidence> where TEvidence : class
{
    private ProviderResult(ProviderStatus status, TEvidence? evidence, string? reason)
    {
        Status = status;
        Evidence = evidence;
        Reason = reason;
    }

    public ProviderStatus Status { get; }

    /// <summary>Non-null only when <see cref="Status"/> is <see cref="ProviderStatus.Completed"/>.</summary>
    public TEvidence? Evidence { get; }

    /// <summary>Human-readable cause when <see cref="Status"/> is <see cref="ProviderStatus.Unavailable"/>. Never contains secrets.</summary>
    public string? Reason { get; }

    public static ProviderResult<TEvidence> Completed(TEvidence evidence) =>
        new(ProviderStatus.Completed, evidence, null);

    public static ProviderResult<TEvidence> Pending() =>
        new(ProviderStatus.Pending, null, null);

    public static ProviderResult<TEvidence> Unavailable(string reason) =>
        new(ProviderStatus.Unavailable, null, reason);
}
