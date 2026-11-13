// Tunable numbers of the scan history and its statistics, bound from the "History" section of appsettings.json.
using System.ComponentModel.DataAnnotations;

namespace Tollgate.Application;

public sealed record HistoryOptions
{
    public const string SectionName = "History";

    /// <summary>How long a history entry is kept (set by History:RetentionDays in appsettings.json).</summary>
    [Range(1, 3650)]
    public required int RetentionDays { get; init; }

    /// <summary>Entries returned by the history endpoint when the client does not ask for a number.</summary>
    [Range(1, 1000)]
    public required int DefaultPageSize { get; init; }

    /// <summary>Upper bound for what a client may ask for, so one request cannot dump the whole table.</summary>
    [Range(1, 1000)]
    public required int MaxPageSize { get; init; }

    /// <summary>Length of the "most scanned hosts" list.</summary>
    [Range(1, 100)]
    public required int TopHostsCount { get; init; }
}
