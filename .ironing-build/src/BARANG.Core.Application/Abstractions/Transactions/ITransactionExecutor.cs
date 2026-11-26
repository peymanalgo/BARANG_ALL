using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Abstractions.Transactions;

public interface ITransactionExecutor
{
    Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default);

    Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default);
}
