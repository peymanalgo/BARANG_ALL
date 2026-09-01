using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BARANG.Core.IntegrationTests;

public sealed class CoreApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("BackgroundJobs:ServerEnabled", "false");
        builder.UseSetting(
            "ConnectionStrings:CoreDatabase",
            "Host=unused;Database=unused;Username=unused;Password=unused");
        builder.UseSetting("DatabaseAvailability:WaitForAvailability", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CoreDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<CoreDbContext>>();
            services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("CoreApiTests"));
        });
    }
}
