// URLhaus provider against a fake network: URL status + host record count, and partial failure handling.
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tollgate.Domain.Evidence;
using Tollgate.Providers.UrlHaus;

namespace Tollgate.Providers.Tests;

public class UrlHausProviderTests
{
    private static readonly Uri Target = new("http://malware.example/payload.exe");

    private const string NoResults = """{"query_status":"no_results"}""";
    private const string OnlineUrl = """{"query_status":"ok","url_status":"online","host":"malware.example"}""";
    private const string OfflineUrl = """{"query_status":"ok","url_status":"offline","host":"malware.example"}""";
    private const string HostWith3Records = """{"query_status":"ok","host":"malware.example","url_count":"3","urls":[]}""";

    private static UrlHausProvider CreateProvider(StubHttpHandler handler, string? authKey = "test-key") =>
        new(new HttpClient(handler),
            Options.Create(new UrlHausOptions { AuthKey = authKey, BaseAddress = new Uri("https://urlhaus.test/v1/") }),
            NullLogger<UrlHausProvider>.Instance);

    private static StubHttpHandler Answering(string urlBody, string hostBody) => new(request =>
        request.Path == "/v1/url/" ? StubHttpHandler.Json(urlBody) : StubHttpHandler.Json(hostBody));

    [Fact]
    public async Task Online_url_is_actively_distributing_and_host_records_are_counted()
    {
        var result = await CreateProvider(Answering(OnlineUrl, HostWith3Records)).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(UrlHausUrlStatus.ActivelyDistributing, result.Evidence!.UrlStatus);
        Assert.Equal(3, result.Evidence.HostRecordCount);
    }

    [Fact]
    public async Task Offline_listed_url_is_a_past_record()
    {
        var result = await CreateProvider(Answering(OfflineUrl, HostWith3Records)).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(UrlHausUrlStatus.PastRecord, result.Evidence!.UrlStatus);
    }

    [Fact]
    public async Task Unlisted_url_on_a_host_with_records_reports_both_facts_separately()
    {
        var result = await CreateProvider(Answering(NoResults, HostWith3Records)).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(UrlHausUrlStatus.NotListed, result.Evidence!.UrlStatus);
        Assert.Equal(3, result.Evidence.HostRecordCount);
    }

    [Fact]
    public async Task Nothing_known_is_a_completed_clean_answer_not_unavailable()
    {
        var result = await CreateProvider(Answering(NoResults, NoResults)).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(new UrlHausEvidence(UrlHausUrlStatus.NotListed, 0), result.Evidence);
    }

    [Fact]
    public async Task Sends_url_and_host_lookups_with_the_auth_key_header()
    {
        var handler = Answering(NoResults, NoResults);

        await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        var urlRequest = handler.Requests.Single(r => r.Path == "/v1/url/");
        var hostRequest = handler.Requests.Single(r => r.Path == "/v1/host/");
        Assert.Equal("url=http%3A%2F%2Fmalware.example%2Fpayload.exe", urlRequest.Body);
        Assert.Equal("host=malware.example", hostRequest.Body);
        Assert.All(handler.Requests, r => Assert.Equal("test-key", r.Headers["Auth-Key"]));
    }

    [Fact]
    public async Task Missing_auth_key_is_unavailable_and_sends_no_request()
    {
        var handler = Answering(NoResults, NoResults);

        var result = await CreateProvider(handler, authKey: null).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task One_failed_lookup_makes_the_whole_source_unavailable()
    {
        var handler = new StubHttpHandler(request => request.Path == "/v1/url/"
            ? StubHttpHandler.Json(OnlineUrl)
            : StubHttpHandler.Status(HttpStatusCode.InternalServerError));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Null(result.Evidence);
    }

    [Theory]
    [InlineData("""{"query_status":"invalid_url"}""")]
    [InlineData("""{"query_status":"http_post_expected"}""")]
    [InlineData("null")]
    public async Task Unexpected_query_status_is_unavailable(string body)
    {
        var result = await CreateProvider(Answering(body, NoResults)).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Unauthorized_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(HttpStatusCode.Unauthorized));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }
}
