// A clock the test moves by hand, so cache expiry and quota resets need no real waiting.
namespace Tollgate.Infrastructure.Tests;

internal sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}
