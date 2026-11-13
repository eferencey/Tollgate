// What happened to one source during a scan. Pending and Unavailable are kept apart on purpose:
// "VirusTotal is still analysing, ask again" is a different message than "VirusTotal is down".
using Tollgate.Domain;

namespace Tollgate.Application;

public enum SourceState
{
    /// <summary>The source answered (fresh or from the cache).</summary>
    Completed,

    /// <summary>The source accepted the URL but has no verdict yet; the client should ask again.</summary>
    Pending,

    /// <summary>The source could not answer (timeout, quota, missing key, error). Asking again may or may not help.</summary>
    Unavailable,
}

/// <param name="Reason">Human-readable cause for <see cref="SourceState.Unavailable"/>; never contains secrets.</param>
public sealed record SourceStatus(ThreatSource Source, SourceState State, string? Reason = null);
