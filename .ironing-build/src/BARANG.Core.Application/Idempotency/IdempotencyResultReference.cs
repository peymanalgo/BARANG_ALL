namespace BARANG.Core.Application.Idempotency;

public sealed record IdempotencyResultReference
{
    public IdempotencyResultReference(
        string resultType,
        string referenceId,
        DateTimeOffset completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resultType);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceId);
        if (completedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Idempotency timestamps must use UTC.", nameof(completedAtUtc));
        }

        ResultType = resultType;
        ReferenceId = referenceId;
        CompletedAtUtc = completedAtUtc;
    }

    public string ResultType { get; }

    public string ReferenceId { get; }

    public DateTimeOffset CompletedAtUtc { get; }
}
