// .env parsing and the mapping of VT_API_KEY / GSB_API_KEY / URLHAUS_AUTH_KEY to the provider settings.
using Microsoft.Extensions.Configuration;
using Tollgate.Api.Configuration;
using Tollgate.Application;

namespace Tollgate.Api.Tests;

public class EnvironmentConfigurationTests : IDisposable
{
    private readonly string tempRoot = Directory.CreateTempSubdirectory("tollgate-env-").FullName;

    public void Dispose() => Directory.Delete(tempRoot, recursive: true);

    [Fact]
    public void Dotenv_lines_are_parsed_ignoring_comments_blanks_and_quotes()
    {
        var parsed = DotEnvParser.Parse(
        [
            "# a comment",
            "",
            "VT_API_KEY=abc123",
            "  GSB_API_KEY = \"quoted value\"  ",
            "URLHAUS_AUTH_KEY=",
            "SINGLE='single quoted'",
            "no-equals-sign",
            "KEY_WITH_EQUALS=a=b",
        ]);

        Assert.Equal("abc123", parsed["VT_API_KEY"]);
        Assert.Equal("quoted value", parsed["GSB_API_KEY"]);
        Assert.Equal("", parsed["URLHAUS_AUTH_KEY"]);
        Assert.Equal("single quoted", parsed["SINGLE"]);
        Assert.Equal("a=b", parsed["KEY_WITH_EQUALS"]);
        Assert.Equal(5, parsed.Count);
    }

    [Fact]
    public void A_dotenv_file_in_a_parent_folder_is_found()
    {
        File.WriteAllText(Path.Combine(tempRoot, ".env"), "VT_API_KEY=from-file");
        var nested = Directory.CreateDirectory(Path.Combine(tempRoot, "backend", "src", "Tollgate.Api")).FullName;
        var configuration = new ConfigurationManager();

        configuration.AddDotEnvFile(nested).MapProviderKeys();

        Assert.Equal("from-file", configuration["Providers:VirusTotal:ApiKey"]);
    }

    [Fact]
    public void Real_environment_values_win_over_the_dotenv_file()
    {
        File.WriteAllText(Path.Combine(tempRoot, ".env"), "VT_API_KEY=from-file\nGSB_API_KEY=file-gsb");
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["VT_API_KEY"] = "from-environment" });

        configuration.AddDotEnvFile(tempRoot).MapProviderKeys();

        Assert.Equal("from-environment", configuration["Providers:VirusTotal:ApiKey"]);
        Assert.Equal("file-gsb", configuration["Providers:SafeBrowsing:ApiKey"]);
    }

    [Fact]
    public void Without_a_dotenv_file_nothing_changes()
    {
        var configuration = new ConfigurationManager();

        configuration.AddDotEnvFile(tempRoot).MapProviderKeys();

        Assert.Null(configuration["Providers:VirusTotal:ApiKey"]);
    }

    [Fact]
    public void All_three_variables_map_to_their_provider_settings()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VT_API_KEY"] = "vt",
            ["GSB_API_KEY"] = "gsb",
            ["URLHAUS_AUTH_KEY"] = "uh",
        });

        configuration.MapProviderKeys();

        Assert.Equal("vt", configuration["Providers:VirusTotal:ApiKey"]);
        Assert.Equal("gsb", configuration["Providers:SafeBrowsing:ApiKey"]);
        Assert.Equal("uh", configuration["Providers:UrlHaus:AuthKey"]);
    }

    [Fact]
    public void Blank_variables_are_not_mapped()
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["VT_API_KEY"] = "   ", ["GSB_API_KEY"] = "" });

        configuration.MapProviderKeys();

        Assert.Null(configuration["Providers:VirusTotal:ApiKey"]);
        Assert.Equal(ScanMode.Demo, configuration.ResolveScanMode());
    }

    [Theory]
    [InlineData("Providers:VirusTotal:ApiKey")]
    [InlineData("Providers:SafeBrowsing:ApiKey")]
    [InlineData("Providers:UrlHaus:AuthKey")]
    public void Any_single_key_makes_the_scan_live(string setting)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { [setting] = "some-key" });

        Assert.Equal(ScanMode.Live, configuration.ResolveScanMode());
    }

    [Fact]
    public void No_key_at_all_means_demo_mode()
    {
        Assert.Equal(ScanMode.Demo, new ConfigurationManager().ResolveScanMode());
    }
}
