// A clock the test moves by hand, so cache expiry needs no real waiting.
namespace Tollgate.Api.Tests;

internal sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;
}
