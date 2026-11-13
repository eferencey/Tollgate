// Test double for the network: answers every request from a lambda and records what was sent,
// so provider tests need no real API keys and no internet.
using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Tollgate.Providers.Tests;

internal sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body)
{
    public string Path => Uri.AbsolutePath;
}

internal sealed class StubHttpHandler(Func<RecordedRequest, HttpResponseMessage> respond) : HttpMessageHandler
{
    private readonly ConcurrentQueue<RecordedRequest> requests = new();

    public IReadOnlyList<RecordedRequest> Requests => requests.ToList();

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Json(string json) => Json(HttpStatusCode.OK, json);

    public static HttpResponseMessage Status(HttpStatusCode status) => new(status);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        var recorded = new RecordedRequest(request.Method, request.RequestUri!, headers, body);
        requests.Enqueue(recorded);
        return respond(recorded);
    }
}
