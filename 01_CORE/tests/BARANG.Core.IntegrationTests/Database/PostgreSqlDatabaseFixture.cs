using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using BARANG.Core.Infrastructure.Context;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class PostgreSqlDatabaseFixture : IAsyncLifetime
{
    public const string CollectionName = "PostgreSQL database";
    public const string ConnectionEnvironmentVariable = "BARANG_TEST_DB_CONNECTION";

    private string _connectionString = string.Empty;
    private string _administrationConnectionString = string.Empty;

    public string ConnectionString => _connectionString;
    public CompanyContext CompanyContext { get; } = new();

    public async ValueTask InitializeAsync()
    {
        string connectionString = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"Set {ConnectionEnvironmentVariable} to an isolated PostgreSQL test database.");

        _administrationConnectionString = ValidateAndNormalize(connectionString);
        await using CoreDbContext context = CreateCoreDbContext(_administrationConnectionString);
        await context.Database.MigrateAsync();
        _connectionString = await ConfigureApplicationRoleAsync(_administrationConnectionString);
        await ResetAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public CoreDbContext CreateCoreDbContext()
        => CreateCoreDbContext(ConnectionString);

    public CoreDbContext CreateAdministrationCoreDbContext()
        => CreateCoreDbContext(_administrationConnectionString);

    private CoreDbContext CreateCoreDbContext(string connectionString)
    {
        DbContextOptionsBuilder<CoreDbContext> builder = new();
        builder.UseCorePostgreSql(connectionString);
        return new CoreDbContext(builder.Options, CompanyContext);
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
                  AND tablename NOT IN ('__ef_migrations_history', 'permissions');

                IF application_tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || application_tables || ' RESTART IDENTITY CASCADE';
                END IF;

                INSERT INTO barang.permissions (id, code, technical_description, module) VALUES
                    ('00000000-0000-0000-0000-000000000001', 'organization.company.manage', 'organization.company.manage', 'organization'),
                    ('00000000-0000-0000-0000-000000000002', 'organization.branch.manage', 'organization.branch.manage', 'organization'),
                    ('00000000-0000-0000-0000-000000000003', 'organization.settings.manage', 'organization.settings.manage', 'organization'),
                    ('00000000-0000-0000-0000-000000000004', 'organization.unit.manage', 'organization.unit.manage', 'organization'),
                    ('00000000-0000-0000-0000-000000000005', 'organization.warehouse.manage', 'organization.warehouse.manage', 'organization'),
                    ('00000000-0000-0000-0000-000000000006', 'security.user.manage', 'security.user.manage', 'security'),
                    ('00000000-0000-0000-0000-000000000007', 'security.role.manage', 'security.role.manage', 'security'),
                    ('00000000-0000-0000-0000-000000000008', 'audit.read', 'audit.read', 'audit'),
                    ('00000000-0000-0000-0000-000000000009', 'lookup.read', 'lookup.read', 'lookup'),
                    ('00000000-0000-0000-0000-000000000010', 'lookup.manage', 'lookup.manage', 'lookup'),
                    ('00000000-0000-0000-0000-000000000011', 'customer.read', 'customer.read', 'customer'),
                    ('00000000-0000-0000-0000-000000000012', 'customer.manage', 'customer.manage', 'customer')
                ON CONFLICT (id) DO NOTHING;

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

                GRANT USAGE ON SCHEMA barang_test TO barang_test_app;
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA barang_test TO barang_test_app;
            END
            $reset$;
            """;

        await using NpgsqlConnection connection = new(_administrationConnectionString);
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

    private static async Task<string> ConfigureApplicationRoleAsync(string administrationConnectionString)
    {
        const string roleName = "barang_test_app";
        string password = $"test-{Guid.NewGuid():N}";
        await using NpgsqlConnection connection = new(administrationConnectionString);
        await connection.OpenAsync();
        await using (NpgsqlCommand create = new("DO $role$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'barang_test_app') THEN CREATE ROLE barang_test_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT; END IF; END $role$;", connection))
        {
            await create.ExecuteNonQueryAsync();
        }
        string configureSql = $"ALTER ROLE barang_test_app PASSWORD '{password}'; GRANT USAGE ON SCHEMA barang TO barang_test_app; GRANT SELECT ON TABLE barang.__ef_migrations_history TO barang_test_app; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA barang TO barang_test_app;";
        await using (NpgsqlCommand configure = new(configureSql, connection))
        {
            await configure.ExecuteNonQueryAsync();
        }

        NpgsqlConnectionStringBuilder application = new(administrationConnectionString)
        {
            Username = roleName,
            Password = password,
            ApplicationName = "BARANG database integration tests application role"
        };
        return application.ConnectionString;
    }
}

[CollectionDefinition(PostgreSqlDatabaseFixture.CollectionName, DisableParallelization = true)]
public sealed class PostgreSqlDatabaseGroup : ICollectionFixture<PostgreSqlDatabaseFixture>;
