// Deleting from the history: one scan or all of them, and the link's cached evidence goes with it,
// so a deleted link is stored nowhere and its next scan asks the sources again.
using static Tollgate.Api.Tests.TollgateFactory;

namespace Tollgate.Api.Tests;

public class HistoryDeletionTests
{
    private static async Task<List<(long Id, string Url)>> HistoryAsync(HttpClient client)
    {
        var (_, history) = await GetJsonAsync(client, "/api/history");
        return history.GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("id").GetInt64(), i.GetProperty("url").GetString()!))
            .ToList();
    }

    [Fact]
    public async Task A_request_under_a_foreign_host_name_is_refused_and_deletes_nothing()
    {
        // DNS rebinding: a web page could point its own domain at this machine and send the
        // browser's request here; only local host names are served, so the history survives.
        using var server = new TollgateFactory();
        var client = server.CreateClient();
        await GetJsonAsync(client, ScanPath("example.com"));
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/history");
        request.Headers.Host = "evil.example";

        var response = await client.SendAsync(request);

        Assert.Equal(400, (int)response.StatusCode);
        Assert.Single(await HistoryAsync(client));
    }

    [Fact]
    public async Task Deleting_one_scan_removes_it_from_history_statistics_and_cache()
    {
        using var server = new TollgateFactory();
        var client = server.CreateClient();
        await GetJsonAsync(client, ScanPath("example.com"));
        await GetJsonAsync(client, ScanPath("free-bonus.xyz"));
        var target = (await HistoryAsync(client)).Single(e => e.Url == "https://example.com/");

        var response = await client.DeleteAsync($"/api/history/{target.Id}");

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Equal(["https://free-bonus.xyz/"], (await HistoryAsync(client)).Select(e => e.Url));
        var (_, stats) = await GetJsonAsync(client, "/api/stats");
        Assert.Equal(1, stats.GetProperty("totalScans").GetInt32());

        // The cache forgot the link too: the next scan asks the sources again (and so is recorded again).
        var (_, rescanned) = await GetJsonAsync(client, ScanPath("example.com"));
        Assert.False(rescanned.GetProperty("fromCache").GetBoolean());
        var (_, other) = await GetJsonAsync(client, ScanPath("free-bonus.xyz"));
        Assert.True(other.GetProperty("fromCache").GetBoolean());
    }

    [Fact]
    public async Task Deleting_an_unknown_scan_is_a_404_problem()
    {
        using var server = new TollgateFactory();

        var (status, problem) = await DeleteJsonAsync(server.CreateClient(), "/api/history/999");

        Assert.Equal(404, status);
        Assert.Equal("not_found", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Clearing_the_history_removes_every_scan_and_the_whole_cache()
    {
        using var server = new TollgateFactory();
        var client = server.CreateClient();
        await GetJsonAsync(client, ScanPath("example.com"));
        await GetJsonAsync(client, ScanPath("free-bonus.xyz"));

        var response = await client.DeleteAsync("/api/history");

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Empty(await HistoryAsync(client));
        var (_, stats) = await GetJsonAsync(client, "/api/stats");
        Assert.Equal(0, stats.GetProperty("totalScans").GetInt32());
        var (_, rescanned) = await GetJsonAsync(client, ScanPath("free-bonus.xyz"));
        Assert.False(rescanned.GetProperty("fromCache").GetBoolean());
    }

    private static async Task<(int Status, System.Text.Json.JsonElement Body)> DeleteJsonAsync(HttpClient client, string path)
    {
        using var response = await client.DeleteAsync(path);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return ((int)response.StatusCode, body.RootElement.Clone());
    }
}
