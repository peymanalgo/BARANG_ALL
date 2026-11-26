namespace BARANG.Core.Application.Idempotency;

public sealed record IdempotencyScope
{
    public IdempotencyScope(
        string operation,
        string actorId,
        Guid? companyId = null,
        string? apiClientId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (!operation.All(static character =>
                character is >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '.' or '_'))
        {
            throw new ArgumentException(
                "An idempotency operation must be a lowercase English technical identifier.",
                nameof(operation));
        }

        Operation = operation;
        ActorId = actorId;
        CompanyId = companyId;
        ApiClientId = apiClientId;
    }

    public string Operation { get; }

    public string ActorId { get; }

    public Guid? CompanyId { get; }

    public string? ApiClientId { get; }
}
