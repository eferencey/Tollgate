// Hand-written fakes for the orchestrator tests: scripted providers, a dictionary cache and a settable clock.
using System.Collections.Concurrent;
using Tollgate.Domain;
using Tollgate.Providers;

namespace Tollgate.Application.Tests;

internal sealed class ScriptedProvider<TEvidence>(
    ThreatSource source, Func<Uri, ProviderResult<TEvidence>> respond) : IThreatProvider<TEvidence>
    where TEvidence : class
{
    private int calls;

    public ThreatSource Source => source;

    public int Calls => Volatile.Read(ref calls);

    /// <summary>Awaited before answering; lets a test hold a provider open to prove others run alongside it.</summary>
    public Func<Task>? BeforeAnswer { get; init; }

    public async Task<ProviderResult<TEvidence>> CheckAsync(Uri url, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        if (BeforeAnswer is not null)
        {
            await BeforeAnswer();
        }

        return respond(url);
    }
}

internal sealed class DictionaryCache : IEvidenceCache
{
    private readonly ConcurrentDictionary<string, CachedScan> entries = new();

    public Task<CachedScan?> GetAsync(string normalizedUrl, CancellationToken cancellationToken) =>
        Task.FromResult(entries.GetValueOrDefault(normalizedUrl));

    public Task SetAsync(string normalizedUrl, CachedScan scan, CancellationToken cancellationToken)
    {
        entries[normalizedUrl] = scan;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string normalizedUrl, CancellationToken cancellationToken)
    {
        entries.TryRemove(normalizedUrl, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        entries.Clear();
        return Task.CompletedTask;
    }

    public int Count => entries.Count;
}

/// <summary>Keeps entries in a list; <see cref="Failure"/> makes every call throw, like a locked database would.</summary>
internal sealed class ListHistory : IScanHistory
{
    public List<ScanHistoryEntry> Entries { get; } = [];

    public Exception? Failure { get; init; }

    public Task AddAsync(ScanHistoryEntry entry, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            throw Failure;
        }

        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ScanHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ScanHistoryEntry>>(Entries.AsEnumerable().Reverse().Take(limit).ToList());

    public Task<string?> RemoveAsync(long id, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The orchestrator never deletes history.");

    public Task ClearAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException("The orchestrator never deletes history.");
}

/// <summary>A cache that is broken in both directions, like a locked database.</summary>
internal sealed class ThrowingCache : IEvidenceCache
{
    public Task<CachedScan?> GetAsync(string normalizedUrl, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("db locked");

    public Task SetAsync(string normalizedUrl, CachedScan scan, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("db locked");

    public Task RemoveAsync(string normalizedUrl, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("db locked");

    public Task ClearAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("db locked");
}

internal sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}
