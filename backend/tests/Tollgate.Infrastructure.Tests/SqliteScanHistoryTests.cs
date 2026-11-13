// The SQLite history: newest-first listing, the 90-day retention (hidden for readers, deleted on write) and restart survival.
using Microsoft.EntityFrameworkCore;
using Tollgate.Application;
using Tollgate.Domain;
using static Tollgate.Infrastructure.Tests.TestDatabase;

namespace Tollgate.Infrastructure.Tests;

public class SqliteScanHistoryTests
{
    internal static ScanHistoryEntry Entry(string host, int score, DateTimeOffset at, Verdict? verdict = null) => new(
        0,
        $"https://{host}/page?id=1",
        host,
        score,
        verdict ?? (score >= 80 ? Verdict.Safe : score >= 50 ? Verdict.Suspicious : Verdict.Dangerous),
        Confidence.High,
        at);

    [Fact]
    public async Task An_empty_history_lists_nothing()
    {
        await using var db = await CreateAsync();

        Assert.Empty(await db.History.GetRecentAsync(10, default));
    }

    [Fact]
    public async Task Entries_come_back_newest_first_with_every_field_intact()
    {
        await using var db = await CreateAsync();
        var older = Entry("a.example", 100, Start);
        var newer = Entry("b.example", 12, Start.AddMinutes(5));
        await db.History.AddAsync(older, default);
        await db.History.AddAsync(newer, default);

        var recent = await db.History.GetRecentAsync(10, default);

        Assert.Equal([newer, older], recent.Select(e => e with { Id = 0 }));
        Assert.All(recent, e => Assert.True(e.Id > 0));
        Assert.NotEqual(recent[0].Id, recent[1].Id);
    }

    [Fact]
    public async Task Removing_an_entry_deletes_only_that_one_and_returns_its_url()
    {
        await using var db = await CreateAsync();
        await db.History.AddAsync(Entry("keep.example", 90, Start), default);
        await db.History.AddAsync(Entry("gone.example", 90, Start), default);
        var gone = (await db.History.GetRecentAsync(10, default)).Single(e => e.Host == "gone.example");

        var url = await db.History.RemoveAsync(gone.Id, default);

        Assert.Equal("https://gone.example/page?id=1", url);
        Assert.Equal(["keep.example"], (await db.History.GetRecentAsync(10, default)).Select(e => e.Host));
    }

    [Fact]
    public async Task Removing_an_unknown_id_returns_null_and_changes_nothing()
    {
        await using var db = await CreateAsync();
        await db.History.AddAsync(Entry("keep.example", 90, Start), default);

        Assert.Null(await db.History.RemoveAsync(12345, default));
        Assert.Single(await db.History.GetRecentAsync(10, default));
    }

    [Fact]
    public async Task Clearing_deletes_every_entry()
    {
        await using var db = await CreateAsync();
        await db.History.AddAsync(Entry("a.example", 90, Start), default);
        await db.History.AddAsync(Entry("b.example", 10, Start), default);

        await db.History.ClearAsync(default);

        await using var context = db.NewContext();
        Assert.Equal(0, await context.ScanHistory.CountAsync());
    }

    [Fact]
    public async Task The_limit_keeps_only_the_newest_entries()
    {
        await using var db = await CreateAsync();
        for (var minute = 0; minute < 5; minute++)
        {
            await db.History.AddAsync(Entry($"h{minute}.example", 90, Start.AddMinutes(minute)), default);
        }

        var recent = await db.History.GetRecentAsync(2, default);

        Assert.Equal(["h4.example", "h3.example"], recent.Select(e => e.Host));
    }

    [Fact]
    public async Task Scans_with_the_same_timestamp_keep_their_insertion_order_newest_first()
    {
        await using var db = await CreateAsync();
        await db.History.AddAsync(Entry("first.example", 90, Start), default);
        await db.History.AddAsync(Entry("second.example", 90, Start), default);

        var recent = await db.History.GetRecentAsync(10, default);

        Assert.Equal(["second.example", "first.example"], recent.Select(e => e.Host));
    }

    [Fact]
    public async Task Entries_older_than_the_retention_period_are_hidden_from_readers()
    {
        await using var db = await CreateAsync(retentionDays: 90);
        await db.History.AddAsync(Entry("old.example", 90, Start), default);

        db.Clock.Now = Start.AddDays(89);
        Assert.Single(await db.History.GetRecentAsync(10, default));

        db.Clock.Now = Start.AddDays(90);
        Assert.Empty(await db.History.GetRecentAsync(10, default));
    }

    [Fact]
    public async Task Entries_older_than_the_retention_period_are_deleted_when_a_new_one_is_written()
    {
        await using var db = await CreateAsync(retentionDays: 90);
        await db.History.AddAsync(Entry("old.example", 90, Start), default);

        db.Clock.Now = Start.AddDays(91);
        await db.History.AddAsync(Entry("fresh.example", 90, db.Clock.Now), default);

        await using var context = db.NewContext();
        Assert.Equal(["fresh.example"], await context.ScanHistory.Select(r => r.Host).ToListAsync());
    }

    [Fact]
    public async Task The_history_survives_a_restart()
    {
        var directory = Directory.CreateTempSubdirectory("tollgate-restart-").FullName;
        try
        {
            var entry = Entry("kept.example", 63, Start, Verdict.Suspicious);
            await using (var before = await CreateAsync(directory))
            {
                await before.History.AddAsync(entry, default);
            }

            await using var after = await CreateAsync(directory);

            Assert.Equal([entry], (await after.History.GetRecentAsync(10, default)).Select(e => e with { Id = 0 }));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
