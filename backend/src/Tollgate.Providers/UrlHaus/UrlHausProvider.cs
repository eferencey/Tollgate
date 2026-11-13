// URLhaus: two lookups in parallel, one for the exact URL (is it online / known?) and one for its host
// (how many malware records). If either lookup fails the whole source is "unavailable": half an answer would mislead.
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.UrlHaus;

public sealed class UrlHausProvider(
    HttpClient http,
    IOptions<UrlHausOptions> options,
    ILogger<UrlHausProvider> logger) : ThreatProviderBase<UrlHausEvidence>(logger)
{
    private const string Ok = "ok";
    private const string NoResults = "no_results";
    private const string OnlineUrlStatus = "online";

    // url_count arrives as a JSON string ("3"), hence AllowReadingFromString.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly UrlHausOptions settings = options.Value;

    public override ThreatSource Source => ThreatSource.UrlHaus;

    protected override async Task<ProviderResult<UrlHausEvidence>> QueryAsync(Uri url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AuthKey))
        {
            return MissingApiKey();
        }

        var urlLookup = LookupAsync("url/", "url", url.AbsoluteUri, cancellationToken);
        var hostLookup = LookupAsync("host/", "host", url.IdnHost, cancellationToken);
        await Task.WhenAll(urlLookup, hostLookup);

        return ProviderResult<UrlHausEvidence>.Completed(
            new UrlHausEvidence(ToUrlStatus(await urlLookup), ToHostRecordCount(await hostLookup)));
    }

    private async Task<UrlHausResponse> LookupAsync(string path, string field, string value, CancellationToken cancellationToken)
    {
        using var form = new FormUrlEncodedContent([new(field, value)]);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(settings.BaseAddress, path)) { Content = form };
        request.Headers.Add("Auth-Key", settings.AuthKey);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UrlHausResponse>(Json, cancellationToken)
            ?? throw new InvalidOperationException("URLhaus returned an empty body");
    }

    // A listed URL that is no longer online is a "past record"; anything not online counts as past.
    private static UrlHausUrlStatus ToUrlStatus(UrlHausResponse response) => response.QueryStatus switch
    {
        NoResults => UrlHausUrlStatus.NotListed,
        Ok when response.UrlStatus == OnlineUrlStatus => UrlHausUrlStatus.ActivelyDistributing,
        Ok => UrlHausUrlStatus.PastRecord,
        _ => throw UnexpectedStatus(response),
    };

    private static int ToHostRecordCount(UrlHausResponse response) => response.QueryStatus switch
    {
        NoResults => 0,
        Ok => response.UrlCount,
        _ => throw UnexpectedStatus(response),
    };

    private static InvalidOperationException UnexpectedStatus(UrlHausResponse response) =>
        new($"URLhaus query_status '{response.QueryStatus}' is not a valid answer");

    private sealed record UrlHausResponse(string? QueryStatus, string? UrlStatus, int UrlCount);
}
