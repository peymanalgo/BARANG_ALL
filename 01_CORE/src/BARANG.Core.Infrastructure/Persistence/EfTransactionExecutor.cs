using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;
using BARANG.Core.Application.Abstractions.Companies;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BARANG.Core.Application.Abstractions.Actors;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class EfTransactionExecutor(CoreDbContext dbContext, ICompanyContext companyContext, ICurrentActor currentActor) : ITransactionExecutor
{
    public async Task<Result> ExecuteAsync(
        Func<CancellationToken, Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SetSecurityContextAsync(cancellationToken);
            Result result = await operation(cancellationToken);
            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.Failure(Error.Concurrency("core.database.concurrency_conflict"));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.Failure(Error.Conflict("core.database.unique_conflict"));
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SetSecurityContextAsync(cancellationToken);
            Result<T> result = await operation(cancellationToken);
            if (result.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return result;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.Failure<T>(Error.Concurrency("core.database.concurrency_conflict"));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return Result.Failure<T>(Error.Conflict("core.database.unique_conflict"));
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task SetSecurityContextAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        if (Guid.TryParse(currentActor.Actor.UserId, out Guid userId))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('barang.user_id', {userId.ToString()}, true);", cancellationToken);
        }

        if (companyContext.IsAvailable)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('barang.company_id', {companyContext.RequireCompanyId().ToString()}, true);", cancellationToken);
        }
    }
}
