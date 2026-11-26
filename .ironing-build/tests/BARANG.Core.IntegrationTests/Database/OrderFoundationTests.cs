using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;
[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class OrderFoundationTests(PostgreSqlDatabaseFixture database)
{
 [Fact]public async Task MigrationCreatesOrderTablesPermissionsSequenceAndRls(){await database.ResetAsync(TestContext.Current.CancellationToken);await using CoreDbContext db=database.CreateAdministrationCoreDbContext();string[]tables=await db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('orders','order_items','order_print_operations','order_address_snapshots','order_change_requests','order_timeline_events')").ToArrayAsync(TestContext.Current.CancellationToken);Assert.Equal(6,tables.Length);Assert.Equal(6,await db.Permissions.CountAsync(x=>x.Code.StartsWith("order."),TestContext.Current.CancellationToken));long policies=await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_policies WHERE schemaname='barang' AND tablename IN ('orders','order_items','order_print_operations','order_address_snapshots','order_change_requests','order_timeline_events')").SingleAsync(TestContext.Current.CancellationToken);Assert.Equal(6,policies);long sequence=await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_sequences WHERE schemaname='barang' AND sequencename='order_number_seq'").SingleAsync(TestContext.Current.CancellationToken);Assert.Equal(1,sequence);}
 [Fact]public async Task OrderRlsHidesRowsWithoutCompanyContext(){await database.ResetAsync(TestContext.Current.CancellationToken);await using NpgsqlConnection c=new(database.ConnectionString);await c.OpenAsync(TestContext.Current.CancellationToken);await using NpgsqlCommand command=new("SELECT count(*) FROM barang.orders",c);Assert.Equal(0L,Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken),System.Globalization.CultureInfo.InvariantCulture));}
}
