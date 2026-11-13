// Test double for the VirusTotal request gate: lets every request through (counting them) or refuses like a used-up quota.
using Tollgate.Providers.VirusTotal;

namespace Tollgate.Providers.Tests;

internal sealed class FakeVirusTotalGate(string? refuseWith = null) : IVirusTotalRequestGate
{
    public int Passed { get; private set; }

    public Task WaitForTurnAsync(CancellationToken cancellationToken)
    {
        if (refuseWith is not null)
        {
            throw new VirusTotalThrottledException(refuseWith);
        }

        Passed++;
        return Task.CompletedTask;
    }
}
