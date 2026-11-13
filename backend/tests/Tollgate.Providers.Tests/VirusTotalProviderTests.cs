// VirusTotal provider against a fake network: known URL, 404 -> submit -> poll, pending, and failures.
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Providers.Tests;

public class VirusTotalProviderTests
{
    private static readonly Uri Target = new("https://example.com/path?a=1");

    // base64url (no padding) of "https://example.com/path?a=1"
    private const string TargetId = "aHR0cHM6Ly9leGFtcGxlLmNvbS9wYXRoP2E9MQ";

    private const string KnownReport =
        """{"data":{"attributes":{"last_analysis_stats":{"malicious":3,"suspicious":2,"harmless":60,"undetected":10}}}}""";

    private const string SubmissionAck = """{"data":{"type":"analysis","id":"u-abc-123"}}""";

    private static string Analysis(string status) =>
        """{"data":{"attributes":{"status":"STATUS","stats":{"malicious":1,"suspicious":4,"harmless":50}}}}"""
            .Replace("STATUS", status);

    private static VirusTotalProvider CreateProvider(StubHttpHandler handler, string? apiKey = "test-key", int pollAttempts = 3, IVirusTotalRequestGate? gate = null) =>
        new(new HttpClient(handler),
            Options.Create(new VirusTotalOptions
            {
                ApiKey = apiKey,
                BaseAddress = new Uri("https://vt.test/api/v3/"),
                AnalysisPollAttempts = pollAttempts,
                AnalysisPollDelay = TimeSpan.Zero,
            }),
            gate ?? new FakeVirusTotalGate(),
            NullLogger<VirusTotalProvider>.Instance);

    [Fact]
    public async Task Known_url_returns_engine_counts_from_the_existing_report()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(KnownReport));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(3, result.Evidence!.MaliciousEngines);
        Assert.Equal(2, result.Evidence.SuspiciousEngines);
    }

    [Fact]
    public async Task Looks_up_the_url_by_its_base64url_id_and_sends_the_api_key_header()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(KnownReport));

        await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"/api/v3/urls/{TargetId}", request.Path);
        Assert.Equal("test-key", request.Headers["x-apikey"]);
    }

    [Fact]
    public async Task Unknown_url_is_submitted_then_polled_until_the_analysis_completes()
    {
        var polls = 0;
        var handler = new StubHttpHandler(request => request switch
        {
            { Method.Method: "GET", Path: var p } when p == $"/api/v3/urls/{TargetId}" => StubHttpHandler.Status(HttpStatusCode.NotFound),
            { Method.Method: "POST", Path: "/api/v3/urls" } => StubHttpHandler.Json(SubmissionAck),
            { Path: "/api/v3/analyses/u-abc-123" } => StubHttpHandler.Json(Analysis(++polls < 2 ? "queued" : "completed")),
            _ => StubHttpHandler.Status(HttpStatusCode.BadRequest),
        });

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(1, result.Evidence!.MaliciousEngines);
        Assert.Equal(4, result.Evidence.SuspiciousEngines);
        Assert.Equal(2, polls);

        var submission = handler.Requests.Single(r => r.Method == HttpMethod.Post);
        Assert.Equal("url=https%3A%2F%2Fexample.com%2Fpath%3Fa%3D1", submission.Body);
    }

    [Fact]
    public async Task Analysis_still_running_after_the_poll_budget_is_pending_not_unavailable()
    {
        var handler = new StubHttpHandler(request => request switch
        {
            { Method.Method: "GET", Path: var p } when p.StartsWith("/api/v3/urls/") => StubHttpHandler.Status(HttpStatusCode.NotFound),
            { Method.Method: "POST" } => StubHttpHandler.Json(SubmissionAck),
            _ => StubHttpHandler.Json(Analysis("queued")),
        });

        var result = await CreateProvider(handler, pollAttempts: 3).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Pending, result.Status);
        Assert.Null(result.Evidence);
        Assert.Equal(3, handler.Requests.Count(r => r.Path.StartsWith("/api/v3/analyses/")));
    }

    [Fact]
    public async Task Missing_api_key_is_unavailable_and_sends_no_request()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(KnownReport));

        var result = await CreateProvider(handler, apiKey: null).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Http_error_is_unavailable_never_an_exception(HttpStatusCode status)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Status(status));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.NotNull(result.Reason);
    }

    [Fact]
    public async Task Network_failure_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Malformed_json_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("<html>not json</html>"));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Report_without_analysis_stats_is_unavailable()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json("""{"data":{"attributes":{}}}"""));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
    }

    // Known risk: a report can exist while its analysis has not finished, and then the stats
    // are empty or all zero. That must never be read as "0 engines flagged the URL".
    [Theory]
    [InlineData("""{"data":{"attributes":{"last_analysis_stats":{}}}}""")]
    [InlineData("""{"data":{"attributes":{"last_analysis_stats":{"malicious":0,"suspicious":0,"harmless":0,"undetected":0,"timeout":0}}}}""")]
    public async Task A_report_whose_analysis_has_no_engine_verdicts_yet_is_pending_not_clean(string report)
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(report));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Pending, result.Status);
        Assert.Null(result.Evidence);
    }

    [Fact]
    public async Task A_finished_report_with_zero_malicious_engines_is_a_real_clean_result()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(
            """{"data":{"attributes":{"last_analysis_stats":{"malicious":0,"suspicious":0,"harmless":68,"undetected":24}}}}"""));

        var result = await CreateProvider(handler).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Completed, result.Status);
        Assert.Equal(0, result.Evidence!.MaliciousEngines);
    }

    [Fact]
    public async Task Caller_cancellation_propagates_instead_of_being_swallowed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(KnownReport));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateProvider(handler).CheckAsync(Target, cts.Token));
    }

    [Fact]
    public async Task Every_virustotal_request_passes_the_gate_first()
    {
        var gate = new FakeVirusTotalGate();
        var handler = new StubHttpHandler(request => request switch
        {
            { Method.Method: "GET", Path: var p } when p.StartsWith("/api/v3/urls/") => StubHttpHandler.Status(HttpStatusCode.NotFound),
            { Method.Method: "POST" } => StubHttpHandler.Json(SubmissionAck),
            _ => StubHttpHandler.Json(Analysis("completed")),
        });

        await CreateProvider(handler, gate: gate).CheckAsync(Target, CancellationToken.None);

        // lookup + submission + one poll
        Assert.Equal(3, gate.Passed);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task A_refusing_gate_makes_virustotal_unavailable_with_the_reason_and_sends_no_request()
    {
        var handler = new StubHttpHandler(_ => StubHttpHandler.Json(KnownReport));
        var gate = new FakeVirusTotalGate(refuseWith: "VirusTotal daily quota (500 requests) is used up");

        var result = await CreateProvider(handler, gate: gate).CheckAsync(Target, CancellationToken.None);

        Assert.Equal(ProviderStatus.Unavailable, result.Status);
        Assert.Equal("VirusTotal daily quota (500 requests) is used up", result.Reason);
        Assert.Empty(handler.Requests);
    }
}
