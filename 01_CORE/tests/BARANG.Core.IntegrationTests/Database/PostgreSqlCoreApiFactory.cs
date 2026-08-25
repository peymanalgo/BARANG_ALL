using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class PostgreSqlCoreApiFactory(string connectionString, string environment = "Testing") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:CoreDatabase", connectionString);
        builder.UseSetting("DatabaseAvailability:WaitForAvailability", "false");
        builder.UseSetting("CompanyResolution:AllowDevelopmentHeader", "true");
        builder.UseSetting("CompanyResolution:AllowDevelopmentAdministration", "true");
        builder.UseSetting("SecurityBootstrap:Secret", "test-bootstrap-secret-123456789");
    }
}
