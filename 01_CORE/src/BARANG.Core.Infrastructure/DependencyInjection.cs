using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BARANG.Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("CoreDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'CoreDatabase' is required. Configure ConnectionStrings__CoreDatabase.");

        services.AddDbContext<CoreDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddHealthChecks().AddDbContextCheck<CoreDbContext>(
            "postgresql",
            customTestQuery: static (context, cancellationToken) =>
                context.Database.ProviderName is "Microsoft.EntityFrameworkCore.InMemory"
                    ? Task.FromResult(true)
                    : context.Database.CanConnectAsync(cancellationToken));
        services.Configure<DatabaseAvailabilityOptions>(configuration.GetSection("DatabaseAvailability"));
        services.AddHostedService<DatabaseAvailabilityService>();

        return services;
    }
}
