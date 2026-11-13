// Starts the real API in memory for integration tests. All three API keys are forced empty, so a test run
// is demo mode by default and never reads the developer's own .env or talks to a real service.
// The SQLite files go to a temp folder: a fresh one per factory (deleted on dispose), or a shared one passed in,
// which is how a test "restarts the server" (two factories, one folder).
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Tollgate.Api.Tests;

internal sealed class TollgateFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string> settings = new()
    {
        ["VT_API_KEY"] = "",
        ["GSB_API_KEY"] = "",
        ["URLHAUS_AUTH_KEY"] = "",
    };

    private readonly List<Action<IServiceCollection>> serviceOverrides = [];
    private readonly string? ownedDataDirectory;

    public TollgateFactory(string? sharedDataDirectory = null)
    {
        ownedDataDirectory = sharedDataDirectory is null ? Directory.CreateTempSubdirectory("tollgate-api-").FullName : null;
        settings["Persistence:DataDirectory"] = sharedDataDirectory ?? ownedDataDirectory!;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Dispose can run twice (the factory is disposed by "using" and by the host tear-down), so deleting must be repeatable.
        if (disposing && ownedDataDirectory is not null && Directory.Exists(ownedDataDirectory))
        {
            Directory.Delete(ownedDataDirectory, recursive: true);
        }
    }

    public TollgateFactory With(string key, string value)
    {
        settings[key] = value;
        return this;
    }

    public TollgateFactory WithServices(Action<IServiceCollection> configure)
    {
        serviceOverrides.Add(configure);
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services => serviceOverrides.ForEach(configure => configure(services)));
    }

    public static async Task<(int Status, JsonElement Body)> GetJsonAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        return ((int)response.StatusCode, JsonDocument.Parse(body).RootElement.Clone());
    }

    public static string ScanPath(string url) => $"/api/scan?url={Uri.EscapeDataString(url)}";
}
