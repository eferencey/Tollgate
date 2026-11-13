// Lets the provider ask "may I send one more request?" without knowing how VirusTotal's free-tier limits
// (15 s spacing, 500/day) are enforced; the implementation lives in Infrastructure.
namespace Tollgate.Providers.VirusTotal;

public interface IVirusTotalRequestGate
{
    /// <summary>
    /// Completes when the caller may send exactly one VirusTotal request (it waits for its turn in the queue).
    /// </summary>
    /// <exception cref="VirusTotalThrottledException">The daily quota is used up or the queue is too long to wait for.</exception>
    Task WaitForTurnAsync(CancellationToken cancellationToken);
}
