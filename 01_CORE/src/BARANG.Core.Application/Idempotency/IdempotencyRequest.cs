namespace BARANG.Core.Application.Idempotency;

public sealed record IdempotencyRequest
{
    public IdempotencyRequest(
        IdempotencyKey key,
        IdempotencyScope scope,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(scope);
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));

        Key = key;
        Scope = scope;
        ExpiresAtUtc = expiresAtUtc;
    }

    public IdempotencyKey Key { get; }

    public IdempotencyScope Scope { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Idempotency timestamps must use UTC.", parameterName);
        }
    }
}
