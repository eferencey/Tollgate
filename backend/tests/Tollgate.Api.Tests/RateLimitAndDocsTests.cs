// The per-client request limit and the published API description (OpenAPI document + Swagger UI).
using System.Net;
using System.Text.Json;
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class RateLimitAndDocsTests
{
    [Fact]
    public async Task The_request_after_the_limit_is_a_429_problem_with_retry_after()
    {
        using var factory = new TollgateFactory().With("RateLimiting:PermitLimit", "3");
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            using var allowed = await client.GetAsync(ScanPath($"site{i}.example"));
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var rejected = await client.GetAsync(ScanPath("one-too-many.example"));
        var problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.Equal("rate_limited", problem.GetProperty("code").GetString());
        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter!.Single()) > 0);
        Assert.Equal(problem.GetProperty("retryAfterSeconds").GetInt32(), int.Parse(retryAfter!.Single()));
    }

    [Fact]
    public async Task The_limit_does_not_apply_to_the_documentation_pages()
    {
        using var factory = new TollgateFactory().With("RateLimiting:PermitLimit", "1");
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            using var response = await client.GetAsync("/openapi/v1.json");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task The_openapi_document_describes_the_scan_endpoint_and_its_error_responses()
    {
        using var factory = new TollgateFactory();

        var (status, document) = await GetJsonAsync(factory.CreateClient(), "/openapi/v1.json");

        Assert.Equal(200, status);
        Assert.Equal("Tollgate API", document.GetProperty("info").GetProperty("title").GetString());
        var scan = document.GetProperty("paths").GetProperty("/api/scan").GetProperty("get");
        Assert.Equal("ScanUrl", scan.GetProperty("operationId").GetString());
        var responses = scan.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("429", out _));
        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("ScanResponse", out _));

        // The history and statistics endpoints are documented too, so the frontend client can be generated from this document.
        var paths = document.GetProperty("paths");
        Assert.Equal("ScanHistory", paths.GetProperty("/api/history").GetProperty("get").GetProperty("operationId").GetString());
        Assert.Equal("ScanStatistics", paths.GetProperty("/api/stats").GetProperty("get").GetProperty("operationId").GetString());
        Assert.True(schemas.TryGetProperty("HistoryResponse", out _));
        Assert.True(schemas.TryGetProperty("StatsResponse", out _));
    }

    // The generated TypeScript client trusts this document: a field listed as required must really be sent.
    [Fact]
    public async Task The_openapi_document_marks_omitted_nulls_optional_and_numbers_as_plain_integers()
    {
        using var factory = new TollgateFactory();

        var (_, document) = await GetJsonAsync(factory.CreateClient(), "/openapi/v1.json");

        var scan = document.GetProperty("components").GetProperty("schemas").GetProperty("ScanResponse");
        var required = scan.GetProperty("required").EnumerateArray().Select(name => name.GetString()).ToList();
        Assert.DoesNotContain("cappedBy", required);
        Assert.DoesNotContain("retryAfterSeconds", required);
        Assert.Contains("score", required);

        var scoreType = scan.GetProperty("properties").GetProperty("score").GetProperty("type");
        Assert.Equal(JsonValueKind.String, scoreType.ValueKind); // "integer", not ["integer", "string"]
        Assert.Equal("integer", scoreType.GetString());
    }

    [Fact]
    public async Task The_swagger_ui_is_served()
    {
        using var factory = new TollgateFactory();

        using var response = await factory.CreateClient().GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
