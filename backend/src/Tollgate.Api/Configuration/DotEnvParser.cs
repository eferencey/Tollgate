// Parses the lines of a .env file into key/value pairs. Pure function so it can be unit-tested without touching the disk.
namespace Tollgate.Api.Configuration;

public static class DotEnvParser
{
    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            values[key] = Unquote(line[(separator + 1)..].Trim());
        }

        return values;
    }

    // Accepts KEY="value" and KEY='value' as well as the bare form.
    private static string Unquote(string value) =>
        value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0]
            ? value[1..^1]
            : value;
}
