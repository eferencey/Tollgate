// Validates user input and turns it into one canonical http(s) URL (scan flow step 1).
// Canonical form matters because it is also the cache key: "Example.com" and
// "https://example.com/#top" must hit the same cache entry.
using System.Text.RegularExpressions;

namespace Tollgate.Domain.Urls;

public static class UrlNormalizer
{
    /// <summary>Common practical URL limit (browsers and servers); also bounds what we forward to third parties.</summary>
    public const int MaxLength = 2048;

    private const string DefaultScheme = "https";

    // A leading "scheme:" counts as a scheme unless it is really "host:port" (digits after the colon),
    // so "example.com:8080/x" gets https:// prepended while "javascript:alert(1)" is rejected.
    private static readonly Regex SchemePrefix =
        new(@"^[a-zA-Z][a-zA-Z0-9+.\-]*:(?!\d+(?:[/?#]|$))", RegexOptions.Compiled);

    public static UrlNormalizationResult Normalize(string? input)
    {
        var trimmed = input?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return UrlNormalizationResult.Rejected(UrlRejectionReason.Empty);
        }

        if (trimmed.Length > MaxLength)
        {
            return UrlNormalizationResult.Rejected(UrlRejectionReason.TooLong);
        }

        var candidate = SchemePrefix.IsMatch(trimmed) ? trimmed : $"{DefaultScheme}://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return UrlNormalizationResult.Rejected(UrlRejectionReason.InvalidFormat);
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return UrlNormalizationResult.Rejected(UrlRejectionReason.UnsupportedScheme);
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            return UrlNormalizationResult.Rejected(UrlRejectionReason.InvalidFormat);
        }

        // Uri lower-cases scheme and host and drops default ports; we only need to remove the fragment.
        var withoutFragment = uri.GetComponents(UriComponents.AbsoluteUri & ~UriComponents.Fragment, UriFormat.UriEscaped);
        return UrlNormalizationResult.Valid(withoutFragment);
    }
}
