using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class BusinessProfileFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task MigrationCreatesProfileTablesWithRlsAndImmutableSnapshotGrants()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await database.ResetAsync(ct);
        await using CoreDbContext db = database.CreateAdministrationCoreDbContext();
        string[] expected = ["product_technical_profiles", "order_business_profiles", "print_technical_profiles", "order_item_technical_snapshots"];
        string[] tables = await db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('product_technical_profiles','order_business_profiles','print_technical_profiles','order_item_technical_snapshots')").ToArrayAsync(ct);
        Assert.Equal(expected.Order(), tables.Order());
        long policies = await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_policies WHERE schemaname='barang' AND tablename IN ('product_technical_profiles','order_business_profiles','print_technical_profiles','order_item_technical_snapshots')").SingleAsync(ct);
        Assert.Equal(4, policies);
        bool canUpdateSnapshot = await db.Database.SqlQueryRaw<bool>("SELECT has_table_privilege('barang_test_app','barang.order_item_technical_snapshots','UPDATE') AS \"Value\"").SingleAsync(ct);
        Assert.False(canUpdateSnapshot);
        bool canDeleteProfile = await db.Database.SqlQueryRaw<bool>("SELECT has_table_privilege('barang_test_app','barang.order_business_profiles','DELETE') AS \"Value\"").SingleAsync(ct);
        Assert.False(canDeleteProfile);
    }

    [Fact]
    public async Task ProfileRlsHidesRowsWhenCompanyContextIsAbsent()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        foreach (string table in new[] { "product_technical_profiles", "order_business_profiles", "print_technical_profiles", "order_item_technical_snapshots" })
        {
            await using NpgsqlCommand command = new($"SELECT count(*) FROM barang.{table}", connection);
            Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
