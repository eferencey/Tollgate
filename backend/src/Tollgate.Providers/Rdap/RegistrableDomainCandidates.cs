// Picks which domain names to ask RDAP about for a host, without needing the Public Suffix List.
// RDAP only knows registered domains, so "www.example.com" must be asked as "example.com".
using System.Net;

namespace Tollgate.Providers.Rdap;

internal static class RegistrableDomainCandidates
{
    /// <summary>
    /// The last three labels, then the last two (longest first): "a.b.example.co.uk" -> "example.co.uk", "co.uk",
    /// "a.b.example.com" -> "b.example.com", "example.com". Longest first so "example.co.uk" wins over "co.uk".
    /// A fixed maximum of two lookups also stops a hostname with hundreds of labels from causing hundreds of requests.
    /// Empty for IP addresses and single-label hosts, which have no registration to look up.
    /// </summary>
    public static IReadOnlyList<string> For(string host)
    {
        var bare = host.Trim('[', ']');
        if (IPAddress.TryParse(bare, out _))
        {
            return [];
        }

        var labels = bare.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return labels.Length switch
        {
            < 2 => [],
            2 => [string.Join('.', labels)],
            _ => [string.Join('.', labels[^3..]), string.Join('.', labels[^2..])],
        };
    }
}
