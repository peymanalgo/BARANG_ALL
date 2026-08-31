using System.Net;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Pricing;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class PricingFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task MigrationCreatesPricingTablesPermissionsRlsAndRestrictsDelete()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);await using CoreDbContext db=database.CreateAdministrationCoreDbContext();
        string[] tables=await db.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('tariffs','discount_rules','tax_rules','order_price_snapshots','order_item_price_snapshots','price_component_snapshots')").ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(6,tables.Length);Assert.Equal(3,await db.Permissions.CountAsync(x=>x.Code.StartsWith("pricing."),TestContext.Current.CancellationToken));long policies=await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM pg_policies WHERE schemaname='barang' AND tablename IN ('tariffs','discount_rules','tax_rules','order_price_snapshots','order_item_price_snapshots','price_component_snapshots')").SingleAsync(TestContext.Current.CancellationToken);Assert.Equal(6,policies);
        long deletes=await db.Database.SqlQueryRaw<long>("SELECT count(*) AS \"Value\" FROM information_schema.role_table_grants WHERE grantee='barang_test_app' AND privilege_type='DELETE' AND table_schema='barang' AND table_name IN ('tariffs','discount_rules','tax_rules','order_price_snapshots','order_item_price_snapshots','price_component_snapshots')").SingleAsync(TestContext.Current.CancellationToken);Assert.Equal(0,deletes);
    }

    [Fact]
    public async Task PostgreSqlRejectsOverlappingActiveTariffs()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);Guid company=Guid.NewGuid();DateTimeOffset now=DateTimeOffset.UtcNow;await using CoreDbContext db=database.CreateAdministrationCoreDbContext();db.Companies.Add(Company.Create(company,"PRICE_TEST","Pricing test",now).Value);db.Tariffs.Add(Tariff.Create(company,TariffType.Shipping,"DEFAULT",null,null,null,10,"IRR",new(2026,1,1),new(2026,12,31),now).Value);await db.SaveChangesAsync(TestContext.Current.CancellationToken);db.Tariffs.Add(Tariff.Create(company,TariffType.Shipping,"DEFAULT",null,null,null,12,"IRR",new(2026,6,1),null,now).Value);await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PricingEndpointsRejectAnonymousCallersAndRlsHidesMetadata()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);await using PostgreSqlCoreApiFactory factory=new(database.ConnectionString);using HttpClient client=factory.CreateClient();using HttpResponseMessage tariffs=await client.GetAsync("/api/v1/pricing/tariffs",TestContext.Current.CancellationToken);using HttpResponseMessage pricing=await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}/pricing",TestContext.Current.CancellationToken);Assert.Equal(HttpStatusCode.Unauthorized,tariffs.StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,pricing.StatusCode);
        await using NpgsqlConnection connection=new(database.ConnectionString);await connection.OpenAsync(TestContext.Current.CancellationToken);await using NpgsqlCommand command=new("SELECT count(*) FROM barang.tariffs",connection);Assert.Equal(0L,Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken),System.Globalization.CultureInfo.InvariantCulture));
    }
}
