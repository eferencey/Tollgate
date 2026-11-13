// Google Safe Browsing v4 threatMatches:find. The API answers "{}" for an unlisted URL and a "matches" array for a listed one.
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.SafeBrowsing;

public sealed class SafeBrowsingProvider(
    HttpClient http,
    IOptions<SafeBrowsingOptions> options,
    ILogger<SafeBrowsingProvider> logger) : ThreatProviderBase<SafeBrowsingEvidence>(logger)
{
    private const string ClientId = "tollgate";
    private const string ClientVersion = "1.0.0";

    private static readonly string[] ThreatTypes =
        ["MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly SafeBrowsingOptions settings = options.Value;

    public override ThreatSource Source => ThreatSource.SafeBrowsing;

    protected override async Task<ProviderResult<SafeBrowsingEvidence>> QueryAsync(Uri url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return MissingApiKey();
        }

        var body = new
        {
            client = new { clientId = ClientId, clientVersion = ClientVersion },
            threatInfo = new
            {
                threatTypes = ThreatTypes,
                platformTypes = new[] { "ANY_PLATFORM" },
                threatEntryTypes = new[] { "URL" },
                threatEntries = new[] { new { url = url.AbsoluteUri } },
            },
        };

        // "./" stops "threatMatches:find" from being parsed as a URI scheme.
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(settings.BaseAddress, "./threatMatches:find"))
        {
            Content = JsonContent.Create(body, options: Json),
        };
        // The key travels in a header, not the query string, so it can never leak into logged URLs.
        request.Headers.Add("X-Goog-Api-Key", settings.ApiKey);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SafeBrowsingResponse>(Json, cancellationToken);
        return ProviderResult<SafeBrowsingEvidence>.Completed(new SafeBrowsingEvidence(result?.Matches is { Count: > 0 }));
    }

    private sealed record SafeBrowsingResponse(IReadOnlyList<JsonElement>? Matches);
}
