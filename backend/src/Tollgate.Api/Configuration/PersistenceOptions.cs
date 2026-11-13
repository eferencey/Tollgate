// Where the SQLite file lives, bound from the "Persistence" section of appsettings.json.
using System.ComponentModel.DataAnnotations;
using Tollgate.Application;

namespace Tollgate.Api.Configuration;

public sealed record PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>Folder of the database files; a relative path starts at the API project folder.</summary>
    [Required(AllowEmptyStrings = false)]
    public required string DataDirectory { get; init; }

    /// <summary>
    /// One file per mode: demo scans are canned examples, and they must never be served as real cached
    /// evidence (or counted in real statistics) after API keys are added.
    /// </summary>
    public string DatabasePath(string contentRoot, ScanMode mode) =>
        Path.Combine(contentRoot, DataDirectory, $"tollgate-{mode.ToString().ToLowerInvariant()}.db");
}
