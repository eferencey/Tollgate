// Per-client request limit of the public API, bound from "RateLimiting" (20 requests per minute).
using System.ComponentModel.DataAnnotations;

namespace Tollgate.Api.Configuration;

public sealed record ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 100_000)]
    public required int PermitLimit { get; init; }

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public required TimeSpan Window { get; init; }
}
