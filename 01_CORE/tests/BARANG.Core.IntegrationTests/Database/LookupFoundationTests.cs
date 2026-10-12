using System.Net;
using System.Net.Http.Json;
using BARANG.Core.Domain.Lookups;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

[Collection(PostgreSqlDatabaseFixture.CollectionName)]
public sealed class LookupFoundationTests(PostgreSqlDatabaseFixture database)
{
    [Fact]
    public async Task MigrationCreatesLookupSchemaAndPermissions()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken);
        await using CoreDbContext context=database.CreateAdministrationCoreDbContext();
        string[] tables=await context.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema='barang' AND table_name LIKE 'lookup_%'").ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3,tables.Length); Assert.True(await context.Permissions.AnyAsync(x=>x.Code=="lookup.read",TestContext.Current.CancellationToken)); Assert.True(await context.Permissions.AnyAsync(x=>x.Code=="lookup.manage",TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GlobalRowsAreVisibleWhileRlsIsolatesCompanyRowsAndPoolContextDoesNotLeak()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); Guid a=Guid.NewGuid(),b=Guid.NewGuid(); DateTimeOffset now=DateTimeOffset.UtcNow;
        long baselineGlobal;
        long baselineA;
        long baselineB;
        await using (CoreDbContext admin=database.CreateAdministrationCoreDbContext())
        {
            baselineGlobal=await admin.LookupTypes.IgnoreQueryFilters().CountAsync(x=>x.CompanyId==null,TestContext.Current.CancellationToken);
            admin.Companies.Add(Company.Create(a,"LOOKUP_A","Lookup A",now).Value); admin.Companies.Add(Company.Create(b,"LOOKUP_B","Lookup B",now).Value);
            await admin.SaveChangesAsync(TestContext.Current.CancellationToken);
            baselineGlobal=await admin.LookupTypes.IgnoreQueryFilters().CountAsync(x=>x.CompanyId==null,TestContext.Current.CancellationToken);
            baselineA=await admin.LookupTypes.IgnoreQueryFilters().CountAsync(x=>x.CompanyId==null||x.CompanyId==a,TestContext.Current.CancellationToken);
            baselineB=await admin.LookupTypes.IgnoreQueryFilters().CountAsync(x=>x.CompanyId==null||x.CompanyId==b,TestContext.Current.CancellationToken);
            admin.LookupTypes.AddRange(LookupType.Create(Guid.NewGuid(),null,"global.sample","Global",LookupScope.Global,true,false,now).Value,LookupType.Create(Guid.NewGuid(),a,"company.a.sample","A",LookupScope.Company,false,false,now).Value,LookupType.Create(Guid.NewGuid(),b,"company.b.sample","B",LookupScope.Company,false,false,now).Value);
            await admin.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using NpgsqlConnection connection=new(database.ConnectionString); await connection.OpenAsync(TestContext.Current.CancellationToken);
        Assert.Equal(baselineA+2,await VisibleCount(connection,a)); Assert.Equal(baselineB+2,await VisibleCount(connection,b)); Assert.Equal(baselineGlobal+1,await VisibleCountWithoutCompany(connection)); Assert.Equal(0,await TryRenameOtherCompany(connection,a,b));
    }

    [Fact]
    public async Task LookupEndpointsRequireAuthentication()
    {
        await database.ResetAsync(TestContext.Current.CancellationToken); await using PostgreSqlCoreApiFactory factory=new(database.ConnectionString); using HttpClient client=factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/v1/lookups/types",TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,(await client.PostAsJsonAsync("/api/v1/lookups/types",new {code="test.kind",displayName="Kind"},TestContext.Current.CancellationToken)).StatusCode);
    }

    private static async Task<long> VisibleCount(NpgsqlConnection connection,Guid company)
    {
        await using NpgsqlTransaction transaction=await connection.BeginTransactionAsync(TestContext.Current.CancellationToken); await using NpgsqlCommand set=new("SELECT set_config('barang.company_id', @company, true);",connection,transaction); set.Parameters.AddWithValue("company",company.ToString()); await set.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); await using NpgsqlCommand count=new("SELECT count(*) FROM barang.lookup_types;",connection,transaction); long result=Convert.ToInt64(await count.ExecuteScalarAsync(TestContext.Current.CancellationToken),System.Globalization.CultureInfo.InvariantCulture); await transaction.RollbackAsync(TestContext.Current.CancellationToken); return result;
    }
    private static async Task<long> VisibleCountWithoutCompany(NpgsqlConnection connection)
    {
        await using NpgsqlTransaction transaction=await connection.BeginTransactionAsync(TestContext.Current.CancellationToken); await using NpgsqlCommand count=new("SELECT count(*) FROM barang.lookup_types;",connection,transaction); long result=Convert.ToInt64(await count.ExecuteScalarAsync(TestContext.Current.CancellationToken),System.Globalization.CultureInfo.InvariantCulture); await transaction.RollbackAsync(TestContext.Current.CancellationToken); return result;
    }
    private static async Task<int> TryRenameOtherCompany(NpgsqlConnection connection,Guid currentCompany,Guid targetCompany)
    {
        await using NpgsqlTransaction transaction=await connection.BeginTransactionAsync(TestContext.Current.CancellationToken); await using NpgsqlCommand set=new("SELECT set_config('barang.company_id', @company, true);",connection,transaction); set.Parameters.AddWithValue("company",currentCompany.ToString()); await set.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); await using NpgsqlCommand update=new("UPDATE barang.lookup_types SET display_name='leak' WHERE company_id=@target;",connection,transaction); update.Parameters.AddWithValue("target",targetCompany); int result=await update.ExecuteNonQueryAsync(TestContext.Current.CancellationToken); await transaction.RollbackAsync(TestContext.Current.CancellationToken); return result;
    }
}
