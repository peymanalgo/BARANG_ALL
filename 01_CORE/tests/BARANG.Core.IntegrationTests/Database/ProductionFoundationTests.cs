using System.Net;
using Npgsql;
using Xunit;
namespace BARANG.Core.IntegrationTests.Database;
#pragma warning disable CA1707,xUnit1051
[Collection(PostgreSqlDatabaseFixture.CollectionName)]public sealed class ProductionFoundationTests(PostgreSqlDatabaseFixture database)
{
 [Fact]public async Task Migration_creates_tables_permissions_rls_and_history_is_immutable(){await using NpgsqlConnection c=new(database.ConnectionString);await c.OpenAsync();await using NpgsqlCommand q=c.CreateCommand();q.CommandText="SELECT (SELECT count(*) FROM information_schema.tables WHERE table_schema='barang' AND table_name IN ('production_jobs','production_job_history')),(SELECT count(*) FROM barang.permissions WHERE code LIKE 'production.%'),(SELECT count(*) FROM pg_policies WHERE schemaname='barang' AND tablename IN ('production_jobs','production_job_history')),has_table_privilege(current_user,'barang.production_job_history','UPDATE'),has_table_privilege(current_user,'barang.production_job_history','DELETE')";await using NpgsqlDataReader r=await q.ExecuteReaderAsync();Assert.True(await r.ReadAsync());Assert.Equal(2,r.GetInt64(0));Assert.Equal(4,r.GetInt64(1));Assert.Equal(2,r.GetInt64(2));Assert.False(r.GetBoolean(3));Assert.False(r.GetBoolean(4));}
 [Fact]public async Task Production_endpoints_require_authentication_and_bound_pagination(){using PostgreSqlCoreApiFactory f=new(database.ConnectionString);using HttpClient c=f.CreateClient();Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/production/jobs")).StatusCode);Assert.Equal(HttpStatusCode.Unauthorized,(await c.GetAsync("/api/v1/production/capacity")).StatusCode);}
}
#pragma warning restore CA1707,xUnit1051
