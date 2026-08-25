using Microsoft.EntityFrameworkCore;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Domain.Organization;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class CoreDbContext(DbContextOptions<CoreDbContext> options, ICompanyContext companyContext) : DbContext(options)
{
    public const string DefaultSchema = "barang";
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanySetting> CompanySettings => Set<CompanySetting>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
        modelBuilder.Entity<CompanySetting>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<Branch>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<OrganizationalUnit>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        base.OnModelCreating(modelBuilder);
    }
}
