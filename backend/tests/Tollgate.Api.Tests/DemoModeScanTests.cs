// End-to-end through real HTTP in demo mode (no keys): the spec's three example scores, polling and partial results.
using System.Net;
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class DemoModeScanTests : IDisposable
{
    private readonly TollgateFactory factory = new();

    public void Dispose() => factory.Dispose();

    [Fact]
    public async Task Without_any_key_the_app_runs_in_demo_mode_and_says_so()
    {
        var (status, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("example.com"));

        Assert.Equal(200, status);
        Assert.Equal("Demo", body.GetProperty("mode").GetString());
    }

    [Theory]
    [InlineData("example.com", 100, "Safe")]
    [InlineData("http://free-bonus.xyz/claim", 63, "Suspicious")]
    [InlineData("http://downloads.example.org/setup.exe", 0, "Dangerous")]
    public async Task The_three_example_urls_of_the_spec_get_their_documented_scores(string url, int score, string verdict)
    {
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath(url));

        Assert.Equal(score, body.GetProperty("score").GetInt32());
        Assert.Equal(verdict, body.GetProperty("verdict").GetString());
        Assert.Equal("Complete", body.GetProperty("status").GetString());
        Assert.Equal("High", body.GetProperty("confidence").GetString());
    }

    // The "Try an example" links on the home page; the frontend shows these verdicts before the scan runs.
    [Theory]
    [InlineData("https://wikipedia.org/", 100, "Safe")]
    [InlineData("https://paypa1.com/", 55, "Suspicious")]
    [InlineData("https://testsafebrowsing.appspot.com/s/malware.html", 0, "Dangerous")]
    public async Task The_home_page_examples_read_as_their_expected_verdicts(string url, int score, string verdict)
    {
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath(url));

        Assert.Equal(score, body.GetProperty("score").GetInt32());
        Assert.Equal(verdict, body.GetProperty("verdict").GetString());
    }

    [Fact]
    public async Task Every_deduction_is_listed_with_its_source()
    {
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("http://free-bonus.xyz/claim"));

        var findings = body.GetProperty("findings").EnumerateArray().ToList();
        Assert.Equal(4, findings.Count);
        Assert.Equal(63, 100 - findings.Sum(f => f.GetProperty("pointsDeducted").GetInt32()));
        Assert.All(findings, f =>
        {
            Assert.False(string.IsNullOrWhiteSpace(f.GetProperty("source").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(f.GetProperty("description").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(f.GetProperty("kind").GetString())); // the UI's explanations key on this code
        });
    }

    [Fact]
    public async Task A_look_alike_domain_is_marked_down_by_the_local_analysis_without_adding_a_fifth_source()
    {
        // Every demo source says "clean" for this host; only our own URL-shape check objects.
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("http://paypa1.com/login"));

        var finding = Assert.Single(body.GetProperty("findings").EnumerateArray());
        Assert.Equal("LocalAnalysis", finding.GetProperty("source").GetString());
        Assert.Equal(55, body.GetProperty("score").GetInt32());
        Assert.Equal("Suspicious", body.GetProperty("verdict").GetString());
        Assert.Equal("High", body.GetProperty("confidence").GetString());
        Assert.Equal(4, body.GetProperty("sources").GetArrayLength());
        Assert.DoesNotContain(body.GetProperty("sources").EnumerateArray(), s => s.GetProperty("source").GetString() == "LocalAnalysis");
    }

    [Fact]
    public async Task The_scanned_url_is_returned_in_normalized_form()
    {
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("  Example.COM/Path#section "));

        Assert.Equal("https://example.com/Path", body.GetProperty("url").GetString());
    }

    [Fact]
    public async Task A_second_scan_of_the_same_url_comes_from_the_cache()
    {
        var client = factory.CreateClient();

        var (_, first) = await GetJsonAsync(client, ScanPath("example.com"));
        var (_, second) = await GetJsonAsync(client, ScanPath("https://example.com/#other-fragment"));

        Assert.False(first.GetProperty("fromCache").GetBoolean());
        Assert.True(second.GetProperty("fromCache").GetBoolean());
        Assert.Equal(first.GetProperty("scannedAt").GetString(), second.GetProperty("scannedAt").GetString());
    }

    [Fact]
    public async Task A_source_that_is_still_analysing_gives_a_partial_result_then_completes_on_the_second_call()
    {
        var client = factory.CreateClient();

        var (_, first) = await GetJsonAsync(client, ScanPath("slow-site.com"));
        var (_, second) = await GetJsonAsync(client, ScanPath("slow-site.com"));

        Assert.Equal("Partial", first.GetProperty("status").GetString());
        Assert.Equal(20, first.GetProperty("retryAfterSeconds").GetInt32());
        Assert.Equal("Medium", first.GetProperty("confidence").GetString());
        var virusTotal = first.GetProperty("sources").EnumerateArray().Single(s => s.GetProperty("source").GetString() == "VirusTotal");
        Assert.Equal("Pending", virusTotal.GetProperty("state").GetString());

        Assert.Equal("Complete", second.GetProperty("status").GetString());
        Assert.False(second.TryGetProperty("retryAfterSeconds", out _));
        Assert.Equal("High", second.GetProperty("confidence").GetString());
    }

    [Fact]
    public async Task Unavailable_sources_are_shown_with_a_reason_and_are_not_confused_with_pending()
    {
        var (_, body) = await GetJsonAsync(factory.CreateClient(), ScanPath("outage.example"));

        var sources = body.GetProperty("sources").EnumerateArray().ToDictionary(s => s.GetProperty("source").GetString()!);
        Assert.Equal("Unavailable", sources["SafeBrowsing"].GetProperty("state").GetString());
        Assert.Equal("Unavailable", sources["Rdap"].GetProperty("state").GetString());
        Assert.False(string.IsNullOrWhiteSpace(sources["Rdap"].GetProperty("reason").GetString()));
        Assert.Equal("Completed", sources["VirusTotal"].GetProperty("state").GetString());
        Assert.Equal("Complete", body.GetProperty("status").GetString()); // nothing to wait for, so no re-poll
        Assert.Equal("Medium", body.GetProperty("confidence").GetString());
    }

    [Fact]
    public async Task The_root_redirects_to_the_swagger_ui()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }
}
