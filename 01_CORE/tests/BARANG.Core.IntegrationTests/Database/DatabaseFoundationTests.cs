using System.Net;
using BARANG.Core.Application.Exceptions;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class DatabaseFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public void SafetyGuardRejectsNonTestDatabaseName()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PostgreSqlDatabaseFixture.ValidateAndNormalize(
                "Host=localhost;Database=barang_core;Username=unused;Password=unused"));
    }

    [Fact]
    public async Task InitialMigrationIsAppliedAndSchemaExists()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();

        string[] migrations = context.Database.GetMigrations().ToArray();
        string[] applied = (await context.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken)).ToArray();
        string[] pending = (await context.Database.GetPendingMigrationsAsync(
            TestContext.Current.CancellationToken)).ToArray();

        Assert.NotEmpty(migrations);
        Assert.All(migrations, migration => Assert.Contains(migration, applied));
        Assert.Empty(pending);
        Assert.Contains(migrations, migration => migration.EndsWith("_CreateBarangSchema", StringComparison.Ordinal));
        Assert.Contains(migrations, migration => migration.Contains("Workflow", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(migrations, migration => migration.Contains("InventoryUnit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadinessUsesPostgreSqlWhileLivenessRemainsProcessOnly()
    {
        await using PostgreSqlCoreApiFactory factory = new(database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage live = await client.GetAsync(
            "/health/live",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage ready = await client.GetAsync(
            "/health/ready",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage aggregate = await client.GetAsync(
            "/health",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aggregate.StatusCode);
    }

    [Fact]
    public async Task UnavailableDatabaseFailsReadinessButNotLiveness()
    {
        NpgsqlConnectionStringBuilder unavailable = new(database.ConnectionString)
        {
            Port = 1,
            Timeout = 1,
            CommandTimeout = 1
        };
        await using PostgreSqlCoreApiFactory factory = new(unavailable.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage live = await client.GetAsync(
            "/health/live",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage ready = await client.GetAsync(
            "/health/ready",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
    }

    [Fact]
    public async Task TransactionCommitsSuccessfulResult()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();
        EfTransactionExecutor executor = new(context, database.CompanyContext, new CurrentActorContext());
        Guid id = Guid.NewGuid();

        Result result = await executor.ExecuteAsync(
            async cancellationToken =>
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO barang_test.transaction_probes (id, value) VALUES ({id}, {"committed"});",
                    cancellationToken);
                return Result.Success();
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await database.CountTransactionProbesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TransactionRollsBackFailedResult()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();
        EfTransactionExecutor executor = new(context, database.CompanyContext, new CurrentActorContext());
        Guid id = Guid.NewGuid();

        Result result = await executor.ExecuteAsync(
            async cancellationToken =>
            {
                await context.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO barang_test.transaction_probes (id, value) VALUES ({id}, {"rolled-back"});",
                    cancellationToken);
                return Result.Failure(Error.BusinessRule("core.test.expected_failure"));
            },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(0, await database.CountTransactionProbesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TransactionRollsBackAndPropagatesException()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();
        EfTransactionExecutor executor = new(context, database.CompanyContext, new CurrentActorContext());
        Guid id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync(
                async cancellationToken =>
                {
                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO barang_test.transaction_probes (id, value) VALUES ({id}, {"exception"});",
                        cancellationToken);
                    throw new InvalidOperationException("Expected test exception.");
                },
                TestContext.Current.CancellationToken));

        Assert.Equal(0, await database.CountTransactionProbesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TransactionHonorsCancellationBeforeBeginningWork()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();
        EfTransactionExecutor executor = new(context, database.CompanyContext, new CurrentActorContext());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await executor.ExecuteAsync(
                _ => Task.FromResult(Result.Success()),
                cancellation.Token));

        Assert.Equal(0, await database.CountTransactionProbesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResetRemovesDataAndPreservesMigrations()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using (NpgsqlConnection connection = new(database.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using NpgsqlCommand command = new(
                "INSERT INTO barang_test.transaction_probes (id, value) VALUES (@id, @value);",
                connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("value", "before-reset");
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context = database.CreateCoreDbContext();

        Assert.Equal(0, await database.CountTransactionProbesAsync(TestContext.Current.CancellationToken));
        string[] applied = (await context.Database.GetAppliedMigrationsAsync(
            TestContext.Current.CancellationToken)).ToArray();
        Assert.All(context.Database.GetMigrations(), migration => Assert.Contains(migration, applied));
        Assert.Contains(applied, migration => migration.Contains("Workflow", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StaleVersionFailsWithoutSilentOverwrite()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        Guid id = Guid.NewGuid();

        await using (ConcurrencyProbeDbContext seed = database.CreateConcurrencyDbContext())
        {
            seed.ConcurrencyProbes.Add(new ConcurrencyProbe { Id = id, Value = "initial", Version = 1 });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using ConcurrencyProbeDbContext firstContext = database.CreateConcurrencyDbContext();
        await using ConcurrencyProbeDbContext staleContext = database.CreateConcurrencyDbContext();
        ConcurrencyProbe first = await firstContext.ConcurrencyProbes.SingleAsync(
            probe => probe.Id == id,
            TestContext.Current.CancellationToken);
        ConcurrencyProbe stale = await staleContext.ConcurrencyProbes.SingleAsync(
            probe => probe.Id == id,
            TestContext.Current.CancellationToken);

        first.Value = "first-update";
        first.Version++;
        await firstContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        stale.Value = "stale-update";
        stale.Version++;
        DbUpdateConcurrencyException exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await staleContext.SaveChangesAsync(TestContext.Current.CancellationToken));
        ConcurrencyException mapped = new(innerException: exception);

        await using ConcurrencyProbeDbContext verification = database.CreateConcurrencyDbContext();
        ConcurrencyProbe persisted = await verification.ConcurrencyProbes.AsNoTracking().SingleAsync(
            probe => probe.Id == id,
            TestContext.Current.CancellationToken);

        Assert.Equal("first-update", persisted.Value);
        Assert.Equal(2, persisted.Version);
        Assert.Equal(ErrorCategory.Concurrency, mapped.Error.Category);
        Assert.Same(exception, mapped.InnerException);
    }
}
