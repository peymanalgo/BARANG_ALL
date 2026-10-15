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
        await EnsureMigrationRolesAsync(_administrationConnectionString);
        await using CoreDbContext context = CreateCoreDbContext(_administrationConnectionString);
        await context.Database.MigrateAsync();
        await NormalizeMigrationRolePrivilegesAsync(_administrationConnectionString);
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

                INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
                VALUES('13000000-0000-0000-0000-000000000001',NULL,'payment.method','Payment Method','Global',TRUE,TRUE,FALSE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT DO NOTHING;
                INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                VALUES
                    ('13000000-0000-0000-0000-000000000011','13000000-0000-0000-0000-000000000001',NULL,'cash',10,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                    ('13000000-0000-0000-0000-000000000012','13000000-0000-0000-0000-000000000001',NULL,'bank_transfer',20,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                    ('13000000-0000-0000-0000-000000000013','13000000-0000-0000-0000-000000000001',NULL,'card',30,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                    ('13000000-0000-0000-0000-000000000014','13000000-0000-0000-0000-000000000001',NULL,'online',40,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                    ('13000000-0000-0000-0000-000000000015','13000000-0000-0000-0000-000000000001',NULL,'other',50,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT(lookup_type_id,code) DO NOTHING;
                INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
                VALUES('13000000-0000-0000-0000-000000000002',NULL,'print.method','Print method','Global',TRUE,TRUE,FALSE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT DO NOTHING;
                INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                VALUES('13000000-0000-0000-0000-000000000021','13000000-0000-0000-0000-000000000002',NULL,'screen',10,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT(lookup_type_id,code) DO NOTHING;

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
        string configureSql = $"ALTER ROLE barang_test_app LOGIN PASSWORD '{password}'; GRANT USAGE ON SCHEMA barang TO barang_test_app; GRANT SELECT ON TABLE barang.__ef_migrations_history TO barang_test_app; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA barang TO barang_test_app; REVOKE DELETE ON barang.tariffs, barang.discount_rules, barang.tax_rules, barang.order_price_snapshots, barang.order_item_price_snapshots, barang.price_component_snapshots, barang.payments, barang.order_financial_states, barang.invoices, barang.invoice_snapshots, barang.invoice_item_snapshots, barang.finance_history, barang.stock_transactions, barang.stock_balances, barang.stock_reservations, barang.stock_transfers, barang.stock_shortages, barang.purchase_requests, barang.purchase_request_items, barang.purchase_orders, barang.purchase_order_items, barang.purchase_receipts, barang.purchasing_history, barang.production_jobs, barang.production_job_history, barang.qc_templates, barang.qc_template_criteria, barang.qc_inspections, barang.qc_criteria_results, barang.defect_types, barang.qc_defects, barang.reproduction_requests, barang.qc_history, barang.packages, barang.package_items, barang.shipping_handoffs, barang.shipments, barang.shipment_packages, barang.shipment_status_history, barang.workflow_instances, barang.human_tasks, barang.workflow_sla_policies, barang.workflow_escalations, barang.workflow_history, barang.workflow_reconciliation_issues, barang.customer_business_profiles, barang.product_technical_profiles, barang.order_business_profiles, barang.print_technical_profiles, barang.order_item_technical_snapshots FROM barang_test_app; REVOKE UPDATE ON barang.payments, barang.invoices, barang.invoice_snapshots, barang.invoice_item_snapshots, barang.finance_history, barang.stock_transactions, barang.purchase_receipts, barang.purchasing_history, barang.production_job_history, barang.qc_template_criteria, barang.qc_criteria_results, barang.qc_defects, barang.qc_history, barang.package_items, barang.shipment_packages, barang.shipment_status_history, barang.workflow_escalations, barang.workflow_history, barang.order_item_technical_snapshots FROM barang_test_app;";
        configureSql = configureSql.Replace("barang.customer_business_profiles, ", string.Empty, StringComparison.Ordinal);
        await using (NpgsqlCommand configure = new(configureSql, connection))
        {
            await configure.ExecuteNonQueryAsync();
        }
        await using (NpgsqlCommand sequences = new("GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA barang TO barang_test_app;", connection))
        {
            await sequences.ExecuteNonQueryAsync();
        }

        NpgsqlConnectionStringBuilder application = new(administrationConnectionString)
        {
            Username = roleName,
            Password = password,
            ApplicationName = "BARANG database integration tests application role"
        };
        return application.ConnectionString;
    }

    private static async Task EnsureMigrationRolesAsync(string administrationConnectionString)
    {
        const string sql = """
            DO $roles$
            BEGIN
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'barang_app') THEN
                    CREATE ROLE barang_app NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
                END IF;
                IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    CREATE ROLE barang_test_app NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
                END IF;
            END
            $roles$;
            ALTER ROLE barang_app LOGIN PASSWORD 'barang_app_dev_only';
            """;
        await using NpgsqlConnection connection = new(administrationConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task NormalizeMigrationRolePrivilegesAsync(string administrationConnectionString)
    {
        const string sql = "REVOKE UPDATE ON barang.user_permissions FROM barang_app;";
        await using NpgsqlConnection connection = new(administrationConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(PostgreSqlDatabaseFixture.CollectionName, DisableParallelization = true)]
public sealed class PostgreSqlDatabaseGroup : ICollectionFixture<PostgreSqlDatabaseFixture>;
