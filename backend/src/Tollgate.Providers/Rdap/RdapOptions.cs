// Settings for the RDAP provider (domain registration data, no API key), bound from "Providers:Rdap".
namespace Tollgate.Providers.Rdap;

public sealed record RdapOptions
{
    public const string SectionName = "Providers:Rdap";

    /// <summary>Must end with a slash.</summary>
    public required Uri BaseAddress { get; init; }
}
