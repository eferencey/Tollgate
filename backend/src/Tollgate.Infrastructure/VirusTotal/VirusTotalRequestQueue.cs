// Paces every VirusTotal request: one per MinSpacing, at most DailyLimit per UTC day.
// Each caller reserves the next free time slot under a lock, then sleeps until it arrives (a simple FIFO queue).
using Microsoft.Extensions.Options;
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Infrastructure.VirusTotal;

public sealed class VirusTotalRequestQueue(IOptions<VirusTotalQueueOptions> options, TimeProvider clock) : IVirusTotalRequestGate
{
    private readonly VirusTotalQueueOptions settings = options.Value;
    private readonly object sync = new();
    private DateTimeOffset nextFreeSlot = DateTimeOffset.MinValue;
    private DateOnly countedDay;
    private int usedToday;

    public async Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        var wait = ReserveSlot();
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, clock, cancellationToken);
        }
    }

    /// <summary>
    /// Books the next free slot and returns how long to wait for it. A caller that gives up while waiting
    /// still consumes its slot and quota unit: slightly conservative, and it keeps the bookkeeping trivial.
    /// </summary>
    public TimeSpan ReserveSlot()
    {
        lock (sync)
        {
            var now = clock.GetUtcNow();
            var today = DateOnly.FromDateTime(now.UtcDateTime);
            if (today != countedDay)
            {
                countedDay = today;
                usedToday = 0;
            }

            if (usedToday >= settings.DailyLimit)
            {
                throw new VirusTotalThrottledException(
                    $"VirusTotal daily quota ({settings.DailyLimit} requests) is used up; it resets at 00:00 UTC");
            }

            var slot = nextFreeSlot > now ? nextFreeSlot : now;
            var wait = slot - now;
            if (wait > settings.MaxWait)
            {
                throw new VirusTotalThrottledException("VirusTotal request queue is full; try again shortly");
            }

            usedToday++;
            nextFreeSlot = slot + settings.MinSpacing;
            return wait;
        }
    }
}
