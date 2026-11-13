// The one error format of the API: RFC 9457 problem details (application/problem+json) plus a stable
// machine-readable "code" extension, so the UI can switch on the code instead of parsing English text.
using System.Text.Json;
using Tollgate.Domain.Urls;

namespace Tollgate.Api.Errors;

public static class ApiProblems
{
    public const string CodeKey = "code";

    public static IResult InvalidUrl(UrlRejectionReason reason) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid URL",
            detail: reason switch
            {
                UrlRejectionReason.Empty => "The 'url' query parameter is required.",
                UrlRejectionReason.TooLong => $"The URL is longer than {UrlNormalizer.MaxLength} characters.",
                UrlRejectionReason.UnsupportedScheme => "Only http and https URLs can be scanned.",
                _ => "The value is not a valid URL.",
            },
            type: "urn:tollgate:problem:invalid-url",
            extensions: new Dictionary<string, object?>
            {
                [CodeKey] = "invalid_url",
                ["reason"] = JsonNamingPolicy.CamelCase.ConvertName(reason.ToString()),
            });

    public static IResult RateLimited(int retryAfterSeconds) =>
        Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too many requests",
            detail: $"Request limit reached. Try again in {retryAfterSeconds} seconds.",
            type: "urn:tollgate:problem:rate-limited",
            extensions: new Dictionary<string, object?>
            {
                [CodeKey] = "rate_limited",
                ["retryAfterSeconds"] = retryAfterSeconds,
            });

    /// <summary>
    /// Adds the "code" to problems created by the framework itself (unknown route, unhandled exception),
    /// so every error body has the same shape no matter who produced it.
    /// </summary>
    public static void AddDefaultCode(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        if (problem.Extensions.ContainsKey(CodeKey))
        {
            return;
        }

        problem.Extensions[CodeKey] = problem.Status switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
            StatusCodes.Status429TooManyRequests => "rate_limited",
            >= StatusCodes.Status500InternalServerError => "internal_error",
            _ => "error",
        };
    }
}
