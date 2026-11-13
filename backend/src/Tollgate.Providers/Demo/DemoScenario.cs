// Picks a canned scenario from the URL's shape, so the presentation can show every UI state without API keys.
// The demo providers only fabricate evidence; the verdict still comes from the real scoring engine.
namespace Tollgate.Providers.Demo;

public enum DemoScenario
{
    /// <summary>Established, clean domain: scores 100.</summary>
    Clean,

    /// <summary>The "free bonus" link from the original spec: scores 63.</summary>
    Suspicious,

    /// <summary>The ".exe" download from the original spec: scores 0.</summary>
    Malicious,

    /// <summary>VirusTotal is still analysing on the first request, then answers (exercises polling).</summary>
    SlowVirusTotal,

    /// <summary>Safe Browsing and RDAP are down (exercises the "unavailable" / partial-result state).</summary>
    PartialOutage,
}

public static class DemoScenarios
{
    /// <summary>
    /// Google's official Safe Browsing test host: live Safe Browsing lists its pages on purpose, so the home page's
    /// "Dangerous" example must read as malicious in demo mode too.
    /// </summary>
    public const string SafeBrowsingTestHost = "testsafebrowsing.appspot.com";

    // Hints are checked in this order, so a URL matching several gets the most severe one.
    public static DemoScenario For(Uri url)
    {
        var host = url.Host;

        if (url.AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || host.Contains("malware", StringComparison.OrdinalIgnoreCase)
            || host.Equals(SafeBrowsingTestHost, StringComparison.OrdinalIgnoreCase))
        {
            return DemoScenario.Malicious;
        }

        if (host.EndsWith(".xyz", StringComparison.OrdinalIgnoreCase) || host.Contains("bonus", StringComparison.OrdinalIgnoreCase))
        {
            return DemoScenario.Suspicious;
        }

        if (host.Contains("slow", StringComparison.OrdinalIgnoreCase))
        {
            return DemoScenario.SlowVirusTotal;
        }

        if (host.Contains("outage", StringComparison.OrdinalIgnoreCase))
        {
            return DemoScenario.PartialOutage;
        }

        return DemoScenario.Clean;
    }
}
