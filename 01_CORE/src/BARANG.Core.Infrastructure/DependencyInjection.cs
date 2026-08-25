using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Application.Organization;
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
            options.UseCorePostgreSql(connectionString));

        services.AddHealthChecks()
            .AddCheck(
                "self",
                static () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                tags: ["live"])
            .AddDbContextCheck<CoreDbContext>(
            "postgresql",
            customTestQuery: static (context, cancellationToken) =>
                context.Database.ProviderName is "Microsoft.EntityFrameworkCore.InMemory"
                    ? Task.FromResult(true)
                    : context.Database.CanConnectAsync(cancellationToken),
            tags: ["ready"]);
        services.Configure<DatabaseAvailabilityOptions>(configuration.GetSection("DatabaseAvailability"));
        services.AddHostedService<DatabaseAvailabilityService>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<CorrelationContext>();
        services.AddSingleton<ICorrelationContext>(static provider =>
            provider.GetRequiredService<CorrelationContext>());
        services.AddSingleton<CurrentActorContext>();
        services.AddSingleton<ICurrentActor>(static provider =>
            provider.GetRequiredService<CurrentActorContext>());
        services.AddSingleton<CompanyContext>();
        services.AddSingleton<ICompanyContext>(static provider =>
            provider.GetRequiredService<CompanyContext>());
        services.AddScoped<ITransactionExecutor, EfTransactionExecutor>();
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IOrganizationAdministration, OrganizationAdministration>();

        return services;
    }
}
