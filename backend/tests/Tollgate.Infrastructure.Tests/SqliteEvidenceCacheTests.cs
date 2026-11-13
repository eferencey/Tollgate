// The SQLite cache: a 60-minute lifetime counted from the first scan, lossless evidence round trip,
// no leftovers for expired entries, and survival across a restart.
using Microsoft.EntityFrameworkCore;
using Tollgate.Application;
using Tollgate.Domain.Evidence;
using Tollgate.Infrastructure.Persistence;
using static Tollgate.Infrastructure.Tests.TestDatabase;

namespace Tollgate.Infrastructure.Tests;

public class SqliteEvidenceCacheTests
{
    private const string Url = "https://example.com/";

    private static readonly ScanEvidence SomeEvidence = new(new VirusTotalEvidence(0, 0), null, null, null);

    [Fact]
    public async Task Unknown_url_is_a_miss()
    {
        await using var db = await CreateAsync();

        Assert.Null(await db.Cache.GetAsync(Url, default));
    }

    [Fact]
    public async Task Every_kind_of_evidence_survives_the_round_trip_and_missing_sources_stay_missing()
    {
        await using var db = await CreateAsync();
        var evidence = new ScanEvidence(
            new VirusTotalEvidence(MaliciousEngines: 3, SuspiciousEngines: 2),
            new SafeBrowsingEvidence(IsListed: true),
            new UrlHausEvidence(UrlHausUrlStatus.ActivelyDistributing, HostRecordCount: 7),
            Rdap: null);

        await db.Cache.SetAsync(Url, new CachedScan(evidence, Start), default);
        var stored = await db.Cache.GetAsync(Url, default);

        Assert.Equal(evidence, stored!.Evidence);
        Assert.Equal(Start, stored.ScannedAt);
    }

    [Fact]
    public async Task Rdap_answering_without_a_registration_date_is_not_the_same_as_rdap_not_answering()
    {
        await using var db = await CreateAsync();

        await db.Cache.SetAsync(Url, new CachedScan(new ScanEvidence(null, null, null, new RdapEvidence(DomainAgeDays: null)), Start), default);
        var stored = await db.Cache.GetAsync(Url, default);

        Assert.NotNull(stored!.Evidence.Rdap);
        Assert.Null(stored.Evidence.Rdap.DomainAgeDays);
    }

    [Fact]
    public async Task Evidence_is_returned_within_the_lifetime()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default);

        db.Clock.Now = Start.AddMinutes(59);

        Assert.Equal(SomeEvidence, (await db.Cache.GetAsync(Url, default))!.Evidence);
    }

    [Fact]
    public async Task Evidence_expires_exactly_when_the_lifetime_is_over()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default);

        db.Clock.Now = Start.AddMinutes(60);

        Assert.Null(await db.Cache.GetAsync(Url, default));
    }

    [Fact]
    public async Task Lifetime_counts_from_the_first_scan_not_from_the_latest_update()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default);

        db.Clock.Now = Start.AddMinutes(50);
        await db.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default); // e.g. VirusTotal answered late
        db.Clock.Now = Start.AddMinutes(61);

        Assert.Null(await db.Cache.GetAsync(Url, default));
    }

    [Fact]
    public async Task Setting_the_same_url_again_replaces_the_row_instead_of_adding_one()
    {
        await using var db = await CreateAsync();
        var later = new ScanEvidence(new VirusTotalEvidence(2, 0), null, null, null);

        await db.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default);
        await db.Cache.SetAsync(Url, new CachedScan(later, Start), default);

        await using var context = db.NewContext();
        Assert.Equal(1, await context.CachedEvidence.CountAsync());
        Assert.Equal(later, (await db.Cache.GetAsync(Url, default))!.Evidence);
    }

    [Fact]
    public async Task Entries_are_kept_per_url()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync("https://a.example/", new CachedScan(SomeEvidence, Start), default);

        Assert.Null(await db.Cache.GetAsync("https://b.example/", default));
    }

    [Fact]
    public async Task Expired_rows_are_deleted_when_the_next_entry_is_written()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync("https://old.example/", new CachedScan(SomeEvidence, Start), default);

        db.Clock.Now = Start.AddMinutes(61);
        await db.Cache.SetAsync("https://new.example/", new CachedScan(SomeEvidence, db.Clock.Now), default);

        await using var context = db.NewContext();
        Assert.Equal(["https://new.example/"], await context.CachedEvidence.Select(r => r.Url).ToListAsync());
    }

    [Fact]
    public async Task Removing_a_url_forgets_only_that_url()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync("https://a.example/", new CachedScan(SomeEvidence, Start), default);
        await db.Cache.SetAsync("https://b.example/", new CachedScan(SomeEvidence, Start), default);

        await db.Cache.RemoveAsync("https://a.example/", default);
        await db.Cache.RemoveAsync("https://never-cached.example/", default); // no error when there is nothing to forget

        Assert.Null(await db.Cache.GetAsync("https://a.example/", default));
        Assert.NotNull(await db.Cache.GetAsync("https://b.example/", default));
    }

    [Fact]
    public async Task Clearing_forgets_every_url()
    {
        await using var db = await CreateAsync();
        await db.Cache.SetAsync("https://a.example/", new CachedScan(SomeEvidence, Start), default);
        await db.Cache.SetAsync("https://b.example/", new CachedScan(SomeEvidence, Start), default);

        await db.Cache.ClearAsync(default);

        await using var context = db.NewContext();
        Assert.Equal(0, await context.CachedEvidence.CountAsync());
    }

    [Fact]
    public async Task Unreadable_stored_evidence_counts_as_a_miss_instead_of_an_error()
    {
        await using var db = await CreateAsync();
        await using (var context = db.NewContext())
        {
            context.CachedEvidence.Add(new CachedEvidenceRow { Url = Url, EvidenceJson = "{ this is not json", ScannedAt = Start });
            await context.SaveChangesAsync();
        }

        Assert.Null(await db.Cache.GetAsync(Url, default));
    }

    [Fact]
    public async Task The_cache_survives_a_restart_and_keeps_counting_from_the_original_scan_time()
    {
        var directory = Directory.CreateTempSubdirectory("tollgate-restart-").FullName;
        try
        {
            await using (var before = await CreateAsync(directory))
            {
                await before.Cache.SetAsync(Url, new CachedScan(SomeEvidence, Start), default);
            }

            await using var after = await CreateAsync(directory, new FakeClock(Start.AddMinutes(30)));
            var stored = await after.Cache.GetAsync(Url, default);
            Assert.Equal(SomeEvidence, stored!.Evidence);
            Assert.Equal(Start, stored.ScannedAt);

            after.Clock.Now = Start.AddMinutes(60);
            Assert.Null(await after.Cache.GetAsync(Url, default));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
