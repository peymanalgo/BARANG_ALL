using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class PostgreSqlCoreApiFactory(string connectionString, string environment = "Testing", bool useObjectStorage = false, string? publicCompanyId = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:CoreDatabase", connectionString);
        builder.UseSetting("DatabaseAvailability:WaitForAvailability", "false");
        builder.UseSetting("CompanyResolution:AllowDevelopmentHeader", "true");
        builder.UseSetting("CompanyResolution:AllowDevelopmentAdministration", "true");
        builder.UseSetting("SecurityBootstrap:Secret", "test-bootstrap-secret-123456789");
        builder.UseSetting("BackgroundJobs:ServerEnabled", "false");
        builder.UseSetting("FileStorage:Endpoint", "127.0.0.1:9000");
        builder.UseSetting("FileStorage:AccessKey", "barang_minio_test");
        builder.UseSetting("FileStorage:SecretKey", "barang_minio_test_secret");
        if (useObjectStorage)
        {
            builder.UseSetting("FileStorage:Enabled", "true");
            builder.UseSetting("FileStorage:Endpoint", "127.0.0.1:9000");
            builder.UseSetting("FileStorage:AccessKey", "barang_minio_dev");
            builder.UseSetting("FileStorage:SecretKey", "barang_minio_dev_only_12345");
            builder.UseSetting("FileStorage:Bucket", "barang-phase10-tests");
            builder.UseSetting("FileStorage:MaximumUploadBytes", "1048576");
            builder.UseSetting("FileStorage:PublicEndpoint", "127.0.0.1:9000");
            builder.UseSetting("Catalog:PublicCompanyId", publicCompanyId ?? Guid.NewGuid().ToString("N"));
            builder.UseSetting("FileStorage:PublicBucket", "barang-phase10-tests-public");
            builder.UseSetting("FileStorage:PublicEndpoint", "127.0.0.1:9000");
        }
    }
}
