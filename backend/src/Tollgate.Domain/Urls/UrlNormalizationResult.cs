// Outcome of URL normalization: either a canonical URL or the reason it was rejected.
namespace Tollgate.Domain.Urls;

public sealed record UrlNormalizationResult(string? NormalizedUrl, UrlRejectionReason? Rejection)
{
    public bool IsValid => NormalizedUrl is not null;

    public static UrlNormalizationResult Valid(string normalizedUrl) => new(normalizedUrl, null);

    public static UrlNormalizationResult Rejected(UrlRejectionReason reason) => new(null, reason);
}
