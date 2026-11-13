// Turns the .env file and the short environment variable names (VT_API_KEY, ...) into the
// "Providers:*" settings the providers read, and decides between live and demo mode.
using Tollgate.Application;

namespace Tollgate.Api.Configuration;

public static class ConfigurationExtensions
{
    private const string DotEnvFileName = ".env";

    // Short, documented environment variable name -> the setting the provider reads.
    private static readonly IReadOnlyDictionary<string, string> KeySettings = new Dictionary<string, string>
    {
        ["VT_API_KEY"] = "Providers:VirusTotal:ApiKey",
        ["GSB_API_KEY"] = "Providers:SafeBrowsing:ApiKey",
        ["URLHAUS_AUTH_KEY"] = "Providers:UrlHaus:AuthKey",
    };

    /// <summary>
    /// Loads the first ".env" found in the content root or any parent folder (the repo root in practice).
    /// Real environment variables win: a value already configured is never overwritten by the file.
    /// </summary>
    public static ConfigurationManager AddDotEnvFile(this ConfigurationManager configuration, string contentRoot)
    {
        var file = FindUpwards(contentRoot, DotEnvFileName);
        if (file is null)
        {
            return configuration;
        }

        var missing = DotEnvParser.Parse(File.ReadAllLines(file))
            .Where(pair => configuration[pair.Key] is null)
            .ToDictionary(pair => pair.Key, pair => (string?)pair.Value);

        configuration.AddInMemoryCollection(missing);
        return configuration;
    }

    /// <summary>Copies VT_API_KEY, GSB_API_KEY and URLHAUS_AUTH_KEY to the provider settings, when they are set.</summary>
    public static ConfigurationManager MapProviderKeys(this ConfigurationManager configuration)
    {
        var mapped = KeySettings
            .Where(pair => !string.IsNullOrWhiteSpace(configuration[pair.Key]))
            .ToDictionary(pair => pair.Value, pair => configuration[pair.Key]);

        configuration.AddInMemoryCollection(mapped);
        return configuration;
    }

    /// <summary>
    /// Demo only when no key at all is configured. With even one real key the scan is live and the
    /// sources that lack a key report "unavailable": mixing real and fake answers would mislead.
    /// </summary>
    public static ScanMode ResolveScanMode(this IConfiguration configuration) =>
        KeySettings.Values.Any(setting => !string.IsNullOrWhiteSpace(configuration[setting]))
            ? ScanMode.Live
            : ScanMode.Demo;

    private static string? FindUpwards(string startDirectory, string fileName)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
