using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class PostgreSqlCoreApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CoreDatabase", connectionString);
        builder.UseSetting("DatabaseAvailability:WaitForAvailability", "false");
    }
}
