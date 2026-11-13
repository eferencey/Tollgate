// Settings for the Google Safe Browsing v4 provider, bound from "Providers:SafeBrowsing".
namespace Tollgate.Providers.SafeBrowsing;

public sealed record SafeBrowsingOptions
{
    public const string SectionName = "Providers:SafeBrowsing";

    /// <summary>Comes from the GSB_API_KEY environment variable; empty = provider reports "unavailable".</summary>
    public string? ApiKey { get; init; }

    /// <summary>Must end with a slash.</summary>
    public required Uri BaseAddress { get; init; }
}
