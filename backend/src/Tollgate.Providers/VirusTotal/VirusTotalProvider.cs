// VirusTotal v3: looks up an existing URL report; if VT has never seen the URL (404) it submits it
// and polls the analysis a bounded number of times, then reports "pending" so the UI can ask again.
using System.Buffers.Text;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tollgate.Domain;
using Tollgate.Domain.Evidence;

namespace Tollgate.Providers.VirusTotal;

public sealed class VirusTotalProvider(
    HttpClient http,
    IOptions<VirusTotalOptions> options,
    IVirusTotalRequestGate gate,
    ILogger<VirusTotalProvider> logger) : ThreatProviderBase<VirusTotalEvidence>(logger)
{
    private const string CompletedStatus = "completed";

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly VirusTotalOptions settings = options.Value;

    public override ThreatSource Source => ThreatSource.VirusTotal;

    protected override async Task<ProviderResult<VirusTotalEvidence>> QueryAsync(Uri url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return MissingApiKey();
        }

        try
        {
            return await LookUpAsync(url, cancellationToken);
        }
        catch (VirusTotalThrottledException ex)
        {
            // Our own quota/queue limit, not a failure: report why VirusTotal was skipped.
            return ProviderResult<VirusTotalEvidence>.Unavailable(ex.Message);
        }
    }

    private async Task<ProviderResult<VirusTotalEvidence>> LookUpAsync(Uri url, CancellationToken cancellationToken)
    {
        // VT identifies a URL by its unpadded base64url form.
        var urlId = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(url.AbsoluteUri));

        using var response = await SendAsync(HttpMethod.Get, $"urls/{urlId}", content: null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return await SubmitAndAwaitAsync(url, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<VtResponse<VtUrlAttributes>>(Json, cancellationToken);

        return report?.Data?.Attributes?.LastAnalysisStats switch
        {
            null => ProviderResult<VirusTotalEvidence>.Unavailable("VirusTotal report contains no analysis stats"),

            // A report can exist while its analysis is still running (we submitted the URL on an earlier poll).
            // Reading its empty stats as "0 malicious engines" would score an unchecked URL as clean.
            { HasEngineVerdicts: false } => ProviderResult<VirusTotalEvidence>.Pending(),

            var stats => ProviderResult<VirusTotalEvidence>.Completed(ToEvidence(stats)),
        };
    }

    private async Task<ProviderResult<VirusTotalEvidence>> SubmitAndAwaitAsync(Uri url, CancellationToken cancellationToken)
    {
        using var form = new FormUrlEncodedContent([new("url", url.AbsoluteUri)]);
        using var submission = await SendAsync(HttpMethod.Post, "urls", form, cancellationToken);
        submission.EnsureSuccessStatusCode();

        var submitted = await submission.Content.ReadFromJsonAsync<VtResponse<object>>(Json, cancellationToken);
        if (submitted?.Data?.Id is not { } analysisId)
        {
            return ProviderResult<VirusTotalEvidence>.Unavailable("VirusTotal did not return an analysis id");
        }

        for (var attempt = 0; attempt < settings.AnalysisPollAttempts; attempt++)
        {
            await Task.Delay(settings.AnalysisPollDelay, cancellationToken);

            using var poll = await SendAsync(HttpMethod.Get, $"analyses/{analysisId}", content: null, cancellationToken);
            poll.EnsureSuccessStatusCode();
            var analysis = await poll.Content.ReadFromJsonAsync<VtResponse<VtAnalysisAttributes>>(Json, cancellationToken);

            if (analysis?.Data?.Attributes is { Status: CompletedStatus, Stats: { } stats })
            {
                return ProviderResult<VirusTotalEvidence>.Completed(ToEvidence(stats));
            }
        }

        return ProviderResult<VirusTotalEvidence>.Pending();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativePath, HttpContent? content, CancellationToken cancellationToken)
    {
        // Every VirusTotal request passes this single choke point, so the gate sees all of them.
        await gate.WaitForTurnAsync(cancellationToken);

        using var request = new HttpRequestMessage(method, new Uri(settings.BaseAddress, relativePath)) { Content = content };
        request.Headers.Add("x-apikey", settings.ApiKey);
        return await http.SendAsync(request, cancellationToken);
    }

    private static VirusTotalEvidence ToEvidence(VtStats stats) => new(stats.Malicious, stats.Suspicious);
}
