// Safe Browsing provider against a fake network.
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tollgate.Providers.SafeBrowsing;

namespace Tollgate.Providers.Tests;

public class SafeBrowsingProviderTests
{
    private static readonly Uri Target = new("http://bad.example/download.exe");

    private static SafeBrowsingProvider CreateProvider(StubHttpHandler handler, string? apiKey = "test-key") =>
        new(new HttpClient(handler),
            Options.Create(new SafeBrowsingOptions { ApiKey = apiKey, BaseAddress = new Uri("https://gsb.test/v4/") }),
            NullLogger<SafeBrowsingProvider>.Instance);

    [Fact]
    public async Task Empty_object_means_the_url_is_not_listed()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.False(result.Evidence!.IsListed);
    }

    [Fact]
    public async Task Matches_array_means_the_url_is_listed()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(
            """{"matches":[{"threatType":"MALWARE","platformType":"ANY_PLATFORM","threat":{"url":"http://bad.example/download.exe"}}]}"""));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.True(result.Evidence!.IsListed);
    }

    [Fact]
    public async Task Posts_the_url_to_threatMatches_find_with_the_key_in_a_header_not_in_the_query_string()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));

        await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/v4/threatMatches:find", request.Path);
        Assert.Equal("test-key", request.Headers["X-Goog-Api-Key"]);
        Assert.DoesNotContain("test-key", request.Uri.ToString());
        Assert.Contains("\"url\":\"http://bad.example/download.exe\"", request.Body);
        Assert.Contains("\"threatEntryTypes\":[\"URL\"]", request.Body);
    }

    [Fact]
    public async Task Missing_api_key_is_unavailable_and_sends_no_request()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("{}"));

        var result = await CreateProvider(handler, apiKey: " ").CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Http_error_is_unavailable(HttpStatusCode status)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(status));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Malformed_json_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("oops"));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }
}
