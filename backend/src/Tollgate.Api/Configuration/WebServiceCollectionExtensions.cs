// HTTP-facing setup: JSON shape, the problem-details error format, per-client rate limiting and the OpenAPI document.
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Tollgate.Api.Endpoints;
using Tollgate.Api.Errors;

namespace Tollgate.Api.Configuration;

public static class WebServiceCollectionExtensions
{
    public static IServiceCollection AddTollgateWeb(this IServiceCollection services)
    {
        // Enums travel as readable strings ("Suspicious"), nulls are left out. Numbers stay numbers: the web
        // default also accepts "42" as a number, which would make the OpenAPI type "integer or string".
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
        });

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.AddDefaultCode);

        AddRateLimiting(services);

        services.AddOpenApi(options =>
        {
            options.AddSchemaTransformer<OmittedNullsSchemaTransformer>();
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Tollgate API",
                    Version = "v1",
                    Description = "Checks a URL before you pass through it: several threat-intelligence sources in, one explainable 0-100 trust score out. The score is a risk indicator, not a safety guarantee.",
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    // Fixed window per client IP. The limit is read from options when the limiter is built, so tests can change it.
    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                    ? (int)Math.Ceiling(delay.TotalSeconds)
                    : 1;
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
                await ApiProblems.RateLimited(retryAfter).ExecuteAsync(context.HttpContext);
            };
        });

        services.AddOptions<RateLimiterOptions>().Configure<IOptions<ApiRateLimitOptions>>((limiter, settings) =>
            limiter.AddPolicy(ScanEndpoints.RateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Value.PermitLimit,
                        Window = settings.Value.Window,
                        QueueLimit = 0,
                    })));
    }
}
