using System.Net;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;
#pragma warning disable CA1707,xUnit1051
[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class InventoryFoundationTests(PostgreSqlDatabaseFixture database)
{
 [Fact]public async Task Migration_creates_inventory_tables_permissions_rls_and_immutable_grants()
 {
  await using NpgsqlConnection c=new(database.ConnectionString);await c.OpenAsync();
  await using NpgsqlCommand q=c.CreateCommand();q.CommandText="""
   SELECT (SELECT count(*) FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('stock_transactions','stock_balances','stock_reservations','stock_transfers','stock_shortages','purchase_requests','purchase_request_items','purchase_orders','purchase_order_items','purchase_receipts','purchasing_history')),
          (SELECT count(*) FROM barang.permissions WHERE code IN ('inventory.read','inventory.manage','inventory.adjust','purchasing.read','purchasing.manage')),
          (SELECT count(*) FROM pg_policies WHERE schemaname='barang' AND tablename IN ('stock_transactions','stock_balances','stock_reservations','stock_transfers','stock_shortages','purchase_requests','purchase_request_items','purchase_orders','purchase_order_items','purchase_receipts','purchasing_history')),
          has_table_privilege(current_user,'barang.stock_transactions','UPDATE'),has_table_privilege(current_user,'barang.stock_transactions','DELETE');
   """;await using NpgsqlDataReader r=await q.ExecuteReaderAsync();Assert.True(await r.ReadAsync());Assert.Equal(11,r.GetInt64(0));Assert.Equal(5,r.GetInt64(1));Assert.Equal(11,r.GetInt64(2));Assert.False(r.GetBoolean(3));Assert.False(r.GetBoolean(4));
 }
 [Fact]public async Task Inventory_endpoints_require_authentication()
 {
  using PostgreSqlCoreApiFactory f=new(database.ConnectionString);using HttpClient c=f.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/inventory/balances")).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/purchasing/orders")).StatusCode);
 }
}
#pragma warning restore CA1707,xUnit1051
