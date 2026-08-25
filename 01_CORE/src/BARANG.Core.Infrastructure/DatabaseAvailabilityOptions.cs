namespace BARANG.Core.Infrastructure;

public sealed class DatabaseAvailabilityOptions
{
    public const int DefaultMaximumAttempts = 10;
    public const int DefaultDelaySeconds = 2;

    public bool WaitForAvailability { get; init; } = true;
    public int MaximumAttempts { get; init; } = DefaultMaximumAttempts;
    public int DelaySeconds { get; init; } = DefaultDelaySeconds;
}
