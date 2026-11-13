// Loads the scoring options from the Api's real appsettings.json (linked into the test output).
using Microsoft.Extensions.Configuration;

namespace Tollgate.Scoring.Tests;

internal static class ScoringTestConfig
{
    public static ScoringOptions Options { get; } = Load();

    private static ScoringOptions Load() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build()
            .GetSection(ScoringOptions.SectionName)
            .Get<ScoringOptions>()
        ?? throw new InvalidOperationException("The Scoring section is missing from appsettings.json.");
}
