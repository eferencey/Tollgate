// Turns the evidence of one scan into the JSON stored in the cache table and back.
using System.Text.Json;
using System.Text.Json.Serialization;
using Tollgate.Domain.Evidence;

namespace Tollgate.Infrastructure.Persistence;

internal static class EvidenceJson
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter() } };

    // ScanEvidence also has computed list properties; this record stores just the four facts they are derived from.
    private sealed record Stored(
        VirusTotalEvidence? VirusTotal,
        SafeBrowsingEvidence? SafeBrowsing,
        UrlHausEvidence? UrlHaus,
        RdapEvidence? Rdap);

    public static string Write(ScanEvidence evidence) =>
        JsonSerializer.Serialize(new Stored(evidence.VirusTotal, evidence.SafeBrowsing, evidence.UrlHaus, evidence.Rdap), Options);

    /// <summary>Null when the text cannot be read (e.g. written by an older shape of the evidence): the cache is disposable.</summary>
    public static ScanEvidence? TryRead(string json)
    {
        try
        {
            var stored = JsonSerializer.Deserialize<Stored>(json, Options);
            return stored is null ? null : new ScanEvidence(stored.VirusTotal, stored.SafeBrowsing, stored.UrlHaus, stored.Rdap);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
