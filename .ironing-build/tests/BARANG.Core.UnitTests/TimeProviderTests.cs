using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class TimeProviderTests
{
    [Fact]
    public void TimeProviderOffersDeterministicCanonicalUtcTime()
    {
        DateTimeOffset expected = new(2026, 8, 26, 9, 30, 0, TimeSpan.Zero);
        TimeProvider timeProvider = new FixedTimeProvider(expected);

        Assert.Equal(expected, timeProvider.GetUtcNow());
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
