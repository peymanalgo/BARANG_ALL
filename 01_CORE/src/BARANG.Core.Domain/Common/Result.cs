namespace BARANG.Core.Domain.Common;

public sealed class Result
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A successful result cannot have an error and a failed result must have one.");
        }

        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(true, null);

    public static Result<T> Success<T>(T value) => new(value, null);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }

    public static Result<T> Failure<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, error);
    }
}
