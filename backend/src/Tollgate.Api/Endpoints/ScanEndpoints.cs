// GET /api/scan?url=...: validate and normalize the URL (flow step 1), then hand over to the orchestrator.
// No business logic here: the endpoint only translates between HTTP and the Application layer.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tollgate.Api.Contracts;
using Tollgate.Api.Errors;
using Tollgate.Application;
using Tollgate.Domain.Urls;

namespace Tollgate.Api.Endpoints;

public static class ScanEndpoints
{
    public const string RateLimitPolicy = "api";

    public static IEndpointRouteBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireRateLimiting(RateLimitPolicy);

        api.MapGet("/scan", ScanAsync)
            .WithName("ScanUrl")
            .WithSummary("Scan a URL and return an explainable 0-100 trust score")
            .WithDescription(
                "Queries VirusTotal, Google Safe Browsing, URLhaus and RDAP in parallel. "
                + "When status is 'partial' (a source is still analysing), call again with the same url after retryAfterSeconds; "
                + "sources that already answered are served from the cache.")
            .Produces<ScanResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> ScanAsync(
        [FromQuery] string? url,
        ScanOrchestrator orchestrator,
        ScanEnvironment environment,
        IOptions<ScanOptions> scanOptions,
        CancellationToken cancellationToken)
    {
        var normalized = UrlNormalizer.Normalize(url);
        if (!normalized.IsValid)
        {
            return ApiProblems.InvalidUrl(normalized.Rejection!.Value);
        }

        var outcome = await orchestrator.ScanAsync(normalized.NormalizedUrl!, cancellationToken);
        return Results.Ok(ScanResponse.From(outcome, environment.Mode, scanOptions.Value.PollInterval));
    }
}
