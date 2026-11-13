// Splits a URL's host into readable labels ("xn--pple-43d.com" -> "аpple", "com"), shared by the host-based checks.
using System.Globalization;

namespace Tollgate.Scoring.Heuristics;

internal static class HostLabels
{
    private static readonly IdnMapping Idn = new();

    /// <summary>The host's labels in Unicode form, or an empty list for an IP address (it has no name to inspect).</summary>
    public static IReadOnlyList<string> Of(Uri url)
    {
        if (url.HostNameType != UriHostNameType.Dns)
        {
            return [];
        }

        // IdnHost is always the ASCII ("xn--") form, so one code path covers hosts typed in either form.
        return url.IdnHost.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(ToUnicode).ToList();
    }

    private static string ToUnicode(string label)
    {
        try
        {
            return Idn.GetUnicode(label);
        }
        catch (ArgumentException)
        {
            // Not valid punycode: keep the label as written, it is plain ASCII anyway.
            return label;
        }
    }
}
