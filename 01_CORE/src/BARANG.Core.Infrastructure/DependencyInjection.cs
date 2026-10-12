using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Correlation;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Application.Organization;
using BARANG.Core.Application.Security;
using BARANG.Core.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Infrastructure.Auditing;
using BARANG.Core.Application.Lookups;
using BARANG.Core.Application.Customers;
using BARANG.Core.Application.Catalog;
using BARANG.Core.Application.Files;
using BARANG.Core.Application.Orders;
using BARANG.Core.Infrastructure.Orders;
using BARANG.Core.Infrastructure.Files;
using BARANG.Core.Application.Pricing;
using BARANG.Core.Infrastructure.Pricing;
using BARANG.Core.Application.Finance;
using BARANG.Core.Infrastructure.Finance;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Infrastructure.Inventory;
using BARANG.Core.Application.Production;
using BARANG.Core.Infrastructure.Production;
using BARANG.Core.Application.Quality;
using BARANG.Core.Infrastructure.Quality;
using BARANG.Core.Application.Logistics;
using BARANG.Core.Infrastructure.Logistics;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Infrastructure.Workflow;
using Minio;
using Elsa.Extensions;
using BARANG.Core.Application.Background;
using BARANG.Core.Infrastructure.Background;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;
using BARANG.Core.Application.BusinessAnalysis;
using BARANG.Core.Infrastructure.BusinessAnalysis;

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
        services.AddElsa();
        services.AddHangfire(c=>c.SetDataCompatibilityLevel(CompatibilityLevel.Version_180).UseSimpleAssemblyNameTypeSerializer().UseRecommendedSerializerSettings().UsePostgreSqlStorage(o=>o.UseNpgsqlConnection(connectionString),new PostgreSqlStorageOptions{SchemaName="hangfire",PrepareSchemaIfNecessary=true}));
        bool backgroundServerEnabled=configuration.GetValue("BackgroundJobs:ServerEnabled",true);
        if(backgroundServerEnabled)services.AddHangfireServer(o=>o.WorkerCount=2);
        string redisConnection=configuration.GetConnectionString("Redis")??"localhost:6379,abortConnect=false";
        services.AddSingleton<IConnectionMultiplexer>(_=>ConnectionMultiplexer.Connect(redisConnection));

        IHealthChecksBuilder healthChecks = services.AddHealthChecks()
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
        healthChecks.AddCheck<RedisHealthCheck>("redis",tags:["ready"]);
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IMinioClient>(provider =>
        {
            FileStorageOptions values = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>().Value;
            IMinioClient client = new MinioClient().WithEndpoint(values.Endpoint).WithCredentials(values.AccessKey, values.SecretKey).Build();
            return values.Secure ? client.WithSSL() : client;
        });
        services.AddSingleton<IObjectStorage, MinioObjectStorage>();
        if (configuration.GetValue<bool>($"{FileStorageOptions.SectionName}:Enabled"))
        {
            healthChecks.AddCheck<MinioHealthCheck>("object-storage", tags: ["ready"]);
            services.AddHostedService<MinioBucketInitializer>();
        }
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
        services.AddSingleton<IAuditRequestContext, NullAuditRequestContext>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditReader, AuditReader>();
        services.AddScoped<IOrganizationStore, OrganizationStore>();
        services.AddScoped<IOrganizationAdministration, OrganizationAdministration>();
        services.AddScoped<ILookupStore, LookupStore>();
        services.AddScoped<LookupAdministration>();
        services.AddScoped<ILookupAdministration>(p => p.GetRequiredService<LookupAdministration>());
        services.AddScoped<ILookupOptionReader>(p => p.GetRequiredService<LookupAdministration>());
        services.AddScoped<ICustomerStore, CustomerStore>();
        services.AddScoped<ICustomerAdministration, CustomerAdministration>();
        services.AddScoped<ICustomerDefaultAddressResolver>(sp => (ICustomerDefaultAddressResolver)sp.GetRequiredService<ICustomerAdministration>());
        services.AddScoped<ICatalogStore, CatalogStore>();
        services.AddScoped<ICatalogAdministration, CatalogAdministration>();
        services.AddScoped<IArtworkFileAdministration, ArtworkFileAdministration>();
        services.AddScoped<PricingAdministration>();
        services.AddScoped<IPricingAdministration>(p => p.GetRequiredService<PricingAdministration>());
        services.AddScoped<IOrderPricingEngine>(p => p.GetRequiredService<PricingAdministration>());
        services.AddScoped<IFinanceAdministration, FinanceAdministration>();
        services.AddScoped<InventoryAdministration>();
        services.AddScoped<IInventoryAdministration>(p => p.GetRequiredService<InventoryAdministration>());
        services.AddScoped<IPurchasingAdministration>(p => p.GetRequiredService<InventoryAdministration>());
        services.AddScoped<IProductionAdministration, ProductionAdministration>();
        services.AddScoped<IQualityAdministration, QualityAdministration>();
        services.AddSingleton<ICarrierService, LocalCarrierService>();
        services.AddScoped<ILogisticsAdministration, LogisticsAdministration>();
        services.AddScoped<IWorkflowEngineAdapter, ElsaWorkflowEngineAdapter>();
        services.AddScoped<IWorkflowBusinessCompletionAdapter, WorkflowBusinessCompletionAdapter>();
        services.AddScoped<IWorkflowAdministration, WorkflowAdministration>();
        services.AddSingleton<IApplicationCache,RedisApplicationCache>();services.AddSingleton<LocalNotificationSender>();services.AddSingleton<ISmsSender>(x=>x.GetRequiredService<LocalNotificationSender>());services.AddSingleton<IEmailSender>(x=>x.GetRequiredService<LocalNotificationSender>());services.AddSingleton<IPushSender>(x=>x.GetRequiredService<LocalNotificationSender>());services.AddScoped<INotificationAdministration,NotificationAdministration>();services.AddScoped<IOutboxDispatcher,OutboxDispatcher>();services.AddScoped<OutboxDispatchJob>();services.AddScoped<SlaScheduler>();services.AddScoped<ISlaScheduler>(x=>x.GetRequiredService<SlaScheduler>());services.AddScoped<FileProcessingScheduler>();services.AddScoped<IFileProcessingScheduler>(x=>x.GetRequiredService<FileProcessingScheduler>());if(backgroundServerEnabled)services.AddHostedService<BackgroundRecurringRegistrar>();
        services.AddScoped<IOrderAdministration, OrderAdministration>();
        services.AddScoped<IBusinessProfileAdministration, BusinessProfileAdministration>();
        services.AddSingleton<IFileProcessingQueue, FileProcessingQueue>();
        services.AddHostedService<FileProcessingWorker>();
        services.AddSingleton<IPasswordCredentialService, PasswordCredentialService>();
        services.AddSingleton<ISessionTokenService, SessionTokenService>();
        services.AddScoped<ISecurityStore, SecurityStore>();
        services.AddScoped<IRequestIdentityResolver, RequestIdentityResolver>();
        services.AddScoped<ApiClientAdministration>();services.AddScoped<IApiClientAdministration>(x=>x.GetRequiredService<ApiClientAdministration>());services.AddScoped<IApiClientIdentityResolver>(x=>x.GetRequiredService<ApiClientAdministration>());
        services.AddScoped<SecurityAdministration>();
        services.AddScoped<ISecurityAdministration>(static provider => provider.GetRequiredService<SecurityAdministration>());
        services.AddScoped<IPermissionAuthorizationService>(static provider => provider.GetRequiredService<SecurityAdministration>());

        return services;
    }
}
