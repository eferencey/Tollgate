// The VirusTotal queue: 15 s spacing between requests, a 500/day counter that resets at 00:00 UTC, and a bounded wait.
using Microsoft.Extensions.Options;
using Tollgate.Infrastructure.VirusTotal;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Infrastructure.Tests;

public class VirusTotalRequestQueueTests
{
    private static readonly DateTimeOffset Noon = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static (VirusTotalRequestQueue Queue, FakeClock Clock) Create(int dailyLimit = 500, double maxWaitSeconds = 45)
    {
        var clock = new FakeClock(Noon);
        var options = Options.Create(new VirusTotalQueueOptions
        {
            MinSpacing = TimeSpan.FromSeconds(15),
            DailyLimit = dailyLimit,
            MaxWait = TimeSpan.FromSeconds(maxWaitSeconds),
        });
        return (new VirusTotalRequestQueue(options, clock), clock);
    }

    [Fact]
    public void The_first_request_goes_through_immediately()
    {
        var (queue, _) = Create();

        Assert.Equal(TimeSpan.Zero, queue.ReserveSlot());
    }

    [Fact]
    public void Requests_made_at_once_are_spaced_15_seconds_apart()
    {
        var (queue, _) = Create();

        var waits = new[] { queue.ReserveSlot(), queue.ReserveSlot(), queue.ReserveSlot() };

        Assert.Equal([TimeSpan.Zero, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)], waits);
    }

    [Fact]
    public void Time_passing_shortens_the_wait()
    {
        var (queue, clock) = Create();
        queue.ReserveSlot();

        clock.Now = Noon.AddSeconds(10);

        Assert.Equal(TimeSpan.FromSeconds(5), queue.ReserveSlot());
    }

    [Fact]
    public void A_request_after_a_quiet_period_does_not_wait()
    {
        var (queue, clock) = Create();
        queue.ReserveSlot();

        clock.Now = Noon.AddMinutes(5);

        Assert.Equal(TimeSpan.Zero, queue.ReserveSlot());
    }

    [Fact]
    public void A_queue_longer_than_the_maximum_wait_is_refused_without_using_quota()
    {
        var (queue, _) = Create(maxWaitSeconds: 30);
        queue.ReserveSlot(); // waits 0
        queue.ReserveSlot(); // waits 15
        queue.ReserveSlot(); // waits 30 = allowed

        var refused = Assert.Throws<VirusTotalThrottledException>(() => queue.ReserveSlot()); // would wait 45

        Assert.Contains("queue is full", refused.Message);
    }

    [Fact]
    public void The_daily_quota_is_enforced()
    {
        var (queue, clock) = Create(dailyLimit: 3);
        for (var i = 0; i < 3; i++)
        {
            clock.Now = Noon.AddMinutes(i); // far enough apart that spacing never interferes
            queue.ReserveSlot();
        }

        clock.Now = Noon.AddMinutes(10);
        var refused = Assert.Throws<VirusTotalThrottledException>(() => queue.ReserveSlot());

        Assert.Contains("daily quota (3 requests)", refused.Message);
    }

    [Fact]
    public void The_quota_resets_at_midnight_utc()
    {
        var (queue, clock) = Create(dailyLimit: 1);
        queue.ReserveSlot();
        Assert.Throws<VirusTotalThrottledException>(() => queue.ReserveSlot());

        clock.Now = new DateTimeOffset(2026, 1, 2, 0, 0, 1, TimeSpan.Zero);

        Assert.Equal(TimeSpan.Zero, queue.ReserveSlot());
    }

    [Fact]
    public async Task WaitForTurnAsync_returns_immediately_when_no_waiting_is_needed()
    {
        var (queue, _) = Create();

        await queue.WaitForTurnAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task WaitForTurnAsync_surfaces_a_refusal_as_the_throttled_exception()
    {
        var (queue, _) = Create(dailyLimit: 1);
        await queue.WaitForTurnAsync(CancellationToken.None);

        await Assert.ThrowsAsync<VirusTotalThrottledException>(() => queue.WaitForTurnAsync(CancellationToken.None));
    }
}
