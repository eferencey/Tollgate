// URL validation/normalization: only http(s), scheme added when missing, fragment dropped, junk rejected.
using Tollgate.Domain.Urls;

namespace Tollgate.Domain.Tests;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://example.com/", "https://example.com/")]
    [InlineData("http://example.com/a?b=1", "http://example.com/a?b=1")]
    [InlineData("example.com", "https://example.com/")] // scheme added
    [InlineData("example.com/path?q=1", "https://example.com/path?q=1")]
    [InlineData("  https://example.com  ", "https://example.com/")] // trimmed
    [InlineData("HTTPS://Example.COM/Path", "https://example.com/Path")] // scheme+host lower-cased, path kept
    [InlineData("https://example.com/page#section", "https://example.com/page")] // fragment dropped
    [InlineData("https://example.com#top", "https://example.com/")]
    [InlineData("https://example.com:443/x", "https://example.com/x")] // default port dropped
    [InlineData("http://example.com:8080/x", "http://example.com:8080/x")] // custom port kept
    [InlineData("example.com:8080/x", "https://example.com:8080/x")] // host:port is not a scheme
    [InlineData("http://192.168.0.1/login", "http://192.168.0.1/login")]
    public void Accepts_and_normalizes(string input, string expected)
    {
        var result = UrlNormalizer.Normalize(input);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.NormalizedUrl);
        Assert.Null(result.Rejection);
    }

    [Theory]
    [InlineData("https://Example.com/#a", "example.com")] // equivalent spellings share one canonical form
    [InlineData("example.com/", "https://example.com")]
    public void Equivalent_spellings_produce_the_same_canonical_url(string first, string second)
    {
        Assert.Equal(
            UrlNormalizer.Normalize(first).NormalizedUrl,
            UrlNormalizer.Normalize(second).NormalizedUrl);
    }

    [Theory]
    [InlineData("ftp://example.com/file", UrlRejectionReason.UnsupportedScheme)]
    [InlineData("javascript:alert(1)", UrlRejectionReason.UnsupportedScheme)]
    [InlineData("file:///C:/Windows/win.ini", UrlRejectionReason.UnsupportedScheme)]
    [InlineData("data:text/html,<script>alert(1)</script>", UrlRejectionReason.UnsupportedScheme)]
    [InlineData("mailto:someone@example.com", UrlRejectionReason.UnsupportedScheme)]
    public void Rejects_non_http_schemes(string input, UrlRejectionReason expected)
    {
        var result = UrlNormalizer.Normalize(input);

        Assert.False(result.IsValid);
        Assert.Null(result.NormalizedUrl);
        Assert.Equal(expected, result.Rejection);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_empty_input(string? input)
    {
        Assert.Equal(UrlRejectionReason.Empty, UrlNormalizer.Normalize(input).Rejection);
    }

    [Theory]
    [InlineData("http://")]
    [InlineData("https://exa mple.com")]
    [InlineData("not a url at all")]
    [InlineData("https://")]
    public void Rejects_malformed_urls(string input)
    {
        Assert.Equal(UrlRejectionReason.InvalidFormat, UrlNormalizer.Normalize(input).Rejection);
    }

    [Fact]
    public void Rejects_urls_longer_than_the_maximum()
    {
        var tooLong = "https://example.com/" + new string('a', UrlNormalizer.MaxLength);

        Assert.Equal(UrlRejectionReason.TooLong, UrlNormalizer.Normalize(tooLong).Rejection);
    }

    [Fact]
    public void Accepts_a_url_of_exactly_the_maximum_length()
    {
        const string prefix = "https://example.com/";
        var atLimit = prefix + new string('a', UrlNormalizer.MaxLength - prefix.Length);

        Assert.True(UrlNormalizer.Normalize(atLimit).IsValid);
    }
}
