namespace BARANG.Core.Domain.Common;

public sealed class Result<T>
{
    private readonly Error? _error;
    private readonly T? _value;

    internal Result(T? value, Error? error)
    {
        if (error is null)
        {
            ArgumentNullException.ThrowIfNull(value);
        }

        IsSuccess = error is null;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("A successful result has no error.");

}
