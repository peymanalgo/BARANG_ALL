using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    public const string CollectionName = "PostgreSQL database";
    public const string ConnectionEnvironmentVariable = "BARANG_TEST_DB_CONNECTION";

    private string _connectionString = string.Empty;

    public string ConnectionString => _connectionString;

    public async ValueTask InitializeAsync()
    {
        string connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Set {ConnectionEnvironmentVariable} to an isolated PostgreSQL test database.");

        _connectionString = ValidateAndNormalize(connectionString);
        await using CoreDbContext context = CreateCoreDbContext();
        await context.Database.MigrateAsync();
        await ResetAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public CoreDbContext CreateCoreDbContext()
    {
        DbContextOptionsBuilder<CoreDbContext> builder = new();
        builder.UseCorePostgreSql(ConnectionString);
        return new CoreDbContext(builder.Options);
    }

    public ConcurrencyProbeDbContext CreateConcurrencyDbContext()
    {
        DbContextOptionsBuilder<ConcurrencyProbeDbContext> builder = new();
        builder.UseNpgsql(ConnectionString);
        return new ConcurrencyProbeDbContext(builder.Options);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        const string resetSql = """
            DO $reset$
            DECLARE
                application_tables text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO application_tables
                FROM pg_tables
                WHERE schemaname NOT IN ('pg_catalog', 'information_schema', 'public', 'barang_test')
                  AND tablename <> '__ef_migrations_history';

                IF application_tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || application_tables || ' RESTART IDENTITY CASCADE';
                END IF;

                DROP SCHEMA IF EXISTS barang_test CASCADE;
                CREATE SCHEMA barang_test;

                CREATE TABLE barang_test.transaction_probes
                (
                    id uuid NOT NULL,
                    value text NOT NULL,
                    CONSTRAINT pk_transaction_probes PRIMARY KEY (id)
                );

                CREATE TABLE barang_test.concurrency_probes
                (
                    id uuid NOT NULL,
                    value text NOT NULL,
                    version bigint NOT NULL,
                    CONSTRAINT pk_concurrency_probes PRIMARY KEY (id),
                    CONSTRAINT ck_concurrency_probes_version_non_negative CHECK (version >= 0)
                );
            END
            $reset$;
            """;

        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = new(resetSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<long> CountTransactionProbesAsync(CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlCommand command = new(
            "SELECT count(*) FROM barang_test.transaction_probes;",
            connection);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static string ValidateAndNormalize(string connectionString)
    {
        NpgsqlConnectionStringBuilder builder = new(connectionString);
        string databaseName = builder.Database ?? string.Empty;
        if (!databaseName.StartsWith("barang_", StringComparison.Ordinal) ||
            !databaseName.EndsWith("_test", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Database integration tests require a database named barang_*_test.");
        }

        builder.ApplicationName = "BARANG database integration tests";
        builder.IncludeErrorDetail = false;
        return builder.ConnectionString;
    }
}

[CollectionDefinition(PostgreSqlDatabaseFixture.CollectionName, DisableParallelization = true)]
public sealed class PostgreSqlDatabaseGroup : ICollectionFixture<PostgreSqlDatabaseFixture>;
