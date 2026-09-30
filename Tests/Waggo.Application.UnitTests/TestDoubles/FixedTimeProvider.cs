namespace Waggo.Application.UnitTests.TestDoubles;

/// <summary>Clock frozen at a known instant, so date rules are deterministic.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider()
        : this(Default)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
