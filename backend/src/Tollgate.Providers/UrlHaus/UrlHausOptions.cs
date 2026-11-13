// Settings for the URLhaus (abuse.ch) provider, bound from "Providers:UrlHaus".
namespace Tollgate.Providers.UrlHaus;

public sealed record UrlHausOptions
{
    public const string SectionName = "Providers:UrlHaus";

    /// <summary>Comes from the URLHAUS_AUTH_KEY environment variable; empty = provider reports "unavailable".</summary>
    public string? AuthKey { get; init; }

    /// <summary>Must end with a slash.</summary>
    public required Uri BaseAddress { get; init; }
}
