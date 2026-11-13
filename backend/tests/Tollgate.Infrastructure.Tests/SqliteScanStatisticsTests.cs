// The SQLite statistics: totals, verdict split, dangerous ratio, most scanned hosts, and "no data" for an empty history.
using Tollgate.Application;
using static Tollgate.Infrastructure.Tests.SqliteScanHistoryTests;
using static Tollgate.Infrastructure.Tests.TestDatabase;

namespace Tollgate.Infrastructure.Tests;

public class SqliteScanStatisticsTests
{
    [Fact]
    public async Task An_empty_history_reports_no_data_rather_than_a_score_of_zero()
    {
        await using var db = await CreateAsync();

        var stats = await db.Statistics.GetAsync(default);

        Assert.Equal(0, stats.TotalScans);
        Assert.Null(stats.AverageScore);
        Assert.Null(stats.DangerousRatio);
        Assert.Empty(stats.TopHosts);
    }

    [Fact]
    public async Task Totals_average_and_verdict_split_are_computed_over_the_history()
    {
        await using var db = await CreateAsync();
        await db.History.AddAsync(Entry("safe.example", 100, Start), default);
        await db.History.AddAsync(Entry("odd.example", 63, Start), default);
        await db.History.AddAsync(Entry("bad.example", 0, Start), default);
        await db.History.AddAsync(Entry("worse.example", 5, Start), default);

        var stats = await db.Statistics.GetAsync(default);

        Assert.Equal(4, stats.TotalScans);
        Assert.Equal(42.0, stats.AverageScore);
        Assert.Equal(new VerdictCounts(Dangerous: 2, Suspicious: 1, Safe: 1), stats.Verdicts);
        Assert.Equal(0.5, stats.DangerousRatio);
    }

    [Fact]
    public async Task The_most_scanned_hosts_are_listed_busiest_first_with_ties_in_alphabetical_order_and_capped()
    {
        await using var db = await CreateAsync(); // TopHostsCount is 2 in the test setup
        foreach (var (host, score) in new[] { ("b.example", 100), ("a.example", 40), ("a.example", 60), ("c.example", 0), ("c.example", 0), ("c.example", 30), ("d.example", 90) })
        {
            await db.History.AddAsync(Entry(host, score, Start), default);
        }

        var stats = await db.Statistics.GetAsync(default);

        Assert.Equal(["c.example", "a.example"], stats.TopHosts.Select(h => h.Host));
        Assert.Equal([3, 2], stats.TopHosts.Select(h => h.Scans));
        Assert.Equal(10, stats.TopHosts[0].AverageScore);
        Assert.Equal(50, stats.TopHosts[1].AverageScore);
    }

    [Fact]
    public async Task Entries_past_the_retention_period_are_not_counted()
    {
        await using var db = await CreateAsync(retentionDays: 30);
        await db.History.AddAsync(Entry("old.example", 0, Start), default);
        await db.History.AddAsync(Entry("recent.example", 100, Start.AddDays(25)), default);

        db.Clock.Now = Start.AddDays(40);
        var stats = await db.Statistics.GetAsync(default);

        Assert.Equal(1, stats.TotalScans);
        Assert.Equal(0.0, stats.DangerousRatio);
        Assert.Equal(["recent.example"], stats.TopHosts.Select(h => h.Host));
    }
}
