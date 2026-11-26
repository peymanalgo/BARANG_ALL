namespace BARANG.Core.Application.Idempotency;

public sealed record IdempotencyKey
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public IdempotencyKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length is < MinimumLength or > MaximumLength || !value.All(IsSafeCharacter))
        {
            throw new ArgumentException(
                $"An idempotency key must be {MinimumLength}-{MaximumLength} safe ASCII characters.",
                nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    private static bool IsSafeCharacter(char character) =>
        character is >= 'a' and <= 'z'
        or >= 'A' and <= 'Z'
        or >= '0' and <= '9'
        or '-' or '_' or '.' or ':';
}
