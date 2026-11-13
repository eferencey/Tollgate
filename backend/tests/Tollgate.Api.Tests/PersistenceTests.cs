// Restart proof: the scan history and the evidence cache are in a SQLite file, so they survive a server restart.
// "Restart" = dispose one in-memory app and start a new one on the same data folder.
using Microsoft.Extensions.DependencyInjection;
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class PersistenceTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string dataDirectory = Directory.CreateTempSubdirectory("tollgate-restart-").FullName;

    public void Dispose() => Directory.Delete(dataDirectory, recursive: true);

    private TollgateFactory StartServer(DateTimeOffset? now = null)
    {
        var factory = new TollgateFactory(dataDirectory);
        return now is { } at ? factory.WithServices(services => services.AddSingleton<TimeProvider>(new FakeClock(at))) : factory;
    }

    [Fact]
    public async Task History_and_cache_are_still_there_after_the_server_restarts()
    {
        using (var firstRun = StartServer(Start))
        {
            var client = firstRun.CreateClient();
            await GetJsonAsync(client, ScanPath("example.com"));
            await GetJsonAsync(client, ScanPath("http://free-bonus.xyz/claim"));
        }

        using var secondRun = StartServer(Start.AddMinutes(30));
        var restarted = secondRun.CreateClient();

        // History: both finished scans, newest first.
        var (historyStatus, history) = await GetJsonAsync(restarted, "/api/history");
        var items = history.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(200, historyStatus);
        Assert.Equal(["http://free-bonus.xyz/claim", "https://example.com/"], items.Select(i => i.GetProperty("url").GetString()));
        Assert.Equal([63, 100], items.Select(i => i.GetProperty("score").GetInt32()));

        // Cache: the same URL needs no source call and keeps its original scan time.
        var (_, cached) = await GetJsonAsync(restarted, ScanPath("example.com"));
        Assert.True(cached.GetProperty("fromCache").GetBoolean());
        Assert.Equal(Start, cached.GetProperty("scannedAt").GetDateTimeOffset());
        Assert.Equal(100, cached.GetProperty("score").GetInt32());
    }

    [Fact]
    public async Task The_cache_lifetime_still_counts_from_the_first_scan_after_a_restart()
    {
        using (var firstRun = StartServer(Start))
        {
            await GetJsonAsync(firstRun.CreateClient(), ScanPath("example.com"));
        }

        using var secondRun = StartServer(Start.AddMinutes(61));
        var (_, rescanned) = await GetJsonAsync(secondRun.CreateClient(), ScanPath("example.com"));

        Assert.False(rescanned.GetProperty("fromCache").GetBoolean());
        Assert.Equal(Start.AddMinutes(61), rescanned.GetProperty("scannedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Statistics_are_computed_from_the_persisted_history_after_a_restart()
    {
        using (var firstRun = StartServer(Start))
        {
            var client = firstRun.CreateClient();
            await GetJsonAsync(client, ScanPath("example.com"));
            await GetJsonAsync(client, ScanPath("http://downloads.example.org/setup.exe"));
            await GetJsonAsync(client, ScanPath("http://downloads.example.org/other.exe"));
        }

        using var secondRun = StartServer(Start.AddMinutes(5));
        var (status, stats) = await GetJsonAsync(secondRun.CreateClient(), "/api/stats");

        Assert.Equal(200, status);
        Assert.Equal("Demo", stats.GetProperty("mode").GetString());
        Assert.Equal(3, stats.GetProperty("totalScans").GetInt32());
        Assert.Equal(2, stats.GetProperty("verdicts").GetProperty("dangerous").GetInt32());
        Assert.Equal(1, stats.GetProperty("verdicts").GetProperty("safe").GetInt32());
        Assert.Equal(2.0 / 3, stats.GetProperty("dangerousRatio").GetDouble(), precision: 6);
        var topHost = stats.GetProperty("topHosts").EnumerateArray().First();
        Assert.Equal("downloads.example.org", topHost.GetProperty("host").GetString());
        Assert.Equal(2, topHost.GetProperty("scans").GetInt32());
    }

    [Fact]
    public async Task Provisional_pending_results_are_neither_cached_as_complete_nor_counted_in_the_history()
    {
        using var server = StartServer(Start);
        var client = server.CreateClient();

        var (_, partial) = await GetJsonAsync(client, ScanPath("slow-site.com"));
        var (_, afterPartial) = await GetJsonAsync(client, "/api/history");
        Assert.Equal("Partial", partial.GetProperty("status").GetString());
        Assert.Empty(afterPartial.GetProperty("items").EnumerateArray());

        var (_, complete) = await GetJsonAsync(client, ScanPath("slow-site.com"));
        var (_, afterComplete) = await GetJsonAsync(client, "/api/history");
        Assert.Equal("Complete", complete.GetProperty("status").GetString());
        Assert.Single(afterComplete.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task A_scan_served_entirely_from_the_cache_is_not_added_to_the_history_again()
    {
        using var server = StartServer(Start);
        var client = server.CreateClient();

        await GetJsonAsync(client, ScanPath("example.com"));
        await GetJsonAsync(client, ScanPath("example.com"));
        var (_, history) = await GetJsonAsync(client, "/api/history");

        Assert.Single(history.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Demo_and_live_mode_keep_separate_databases_so_canned_data_never_pretends_to_be_real()
    {
        using (var demo = StartServer(Start))
        {
            await GetJsonAsync(demo.CreateClient(), ScanPath("example.com"));
        }

        using var live = StartServer(Start).With("GSB_API_KEY", "test-key");
        var (_, history) = await GetJsonAsync(live.CreateClient(), "/api/history");

        Assert.Equal("Live", history.GetProperty("mode").GetString());
        Assert.Empty(history.GetProperty("items").EnumerateArray());
        Assert.True(File.Exists(Path.Combine(dataDirectory, "tollgate-demo.db")));
        Assert.True(File.Exists(Path.Combine(dataDirectory, "tollgate-live.db")));
    }

    [Fact]
    public async Task An_empty_history_and_statistics_say_there_is_no_data()
    {
        using var server = StartServer(Start);
        var client = server.CreateClient();

        var (_, history) = await GetJsonAsync(client, "/api/history");
        var (_, stats) = await GetJsonAsync(client, "/api/stats");

        Assert.Empty(history.GetProperty("items").EnumerateArray());
        Assert.Equal(0, stats.GetProperty("totalScans").GetInt32());
        Assert.False(stats.TryGetProperty("averageScore", out _));
        Assert.False(stats.TryGetProperty("dangerousRatio", out _));
        Assert.Equal(90, stats.GetProperty("retentionDays").GetInt32());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(2, 2)]
    [InlineData(100000, 3)] // there are only 3 entries; the cap is checked below with the real maximum
    public async Task The_history_limit_is_clamped_to_what_the_server_allows(int limit, int expectedCount)
    {
        using var server = StartServer(Start);
        var client = server.CreateClient();
        foreach (var url in new[] { "example.com", "free-bonus.xyz", "host/setup.exe" })
        {
            await GetJsonAsync(client, ScanPath(url));
        }

        var (status, history) = await GetJsonAsync(client, $"/api/history?limit={limit}");

        Assert.Equal(200, status);
        Assert.Equal(expectedCount, history.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Without_a_limit_the_default_page_size_applies()
    {
        using var server = StartServer(Start).With("History:DefaultPageSize", "2");
        var client = server.CreateClient();
        foreach (var url in new[] { "example.com", "free-bonus.xyz", "host/setup.exe" })
        {
            await GetJsonAsync(client, ScanPath(url));
        }

        var (_, history) = await GetJsonAsync(client, "/api/history");

        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task The_limit_can_never_exceed_the_configured_maximum()
    {
        using var server = StartServer(Start).With("History:MaxPageSize", "2");
        var client = server.CreateClient();
        foreach (var url in new[] { "example.com", "free-bonus.xyz", "host/setup.exe" })
        {
            await GetJsonAsync(client, ScanPath(url));
        }

        var (_, history) = await GetJsonAsync(client, "/api/history?limit=50");

        Assert.Equal(2, history.GetProperty("items").GetArrayLength());
    }
}
