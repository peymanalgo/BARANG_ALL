using Microsoft.EntityFrameworkCore;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Domain.Organization;
using BARANG.Core.Domain.Security;
using BARANG.Core.Domain.Auditing;

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
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<AuthenticationSession> AuthenticationSessions => Set<AuthenticationSession>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ApiClient> ApiClients => Set<ApiClient>();
    public DbSet<ApiClientCompany> ApiClientCompanies => Set<ApiClientCompany>();
    public DbSet<ApiClientPermission> ApiClientPermissions => Set<ApiClientPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
        modelBuilder.Entity<RolePermission>().HasIndex(entity => entity.PermissionId).HasDatabaseName("ix_role_permissions_permission_id");
        modelBuilder.Entity<UserRole>().HasIndex(entity => entity.RoleId).HasDatabaseName("ix_user_roles_role_id");
        modelBuilder.Entity<AuthenticationSession>().HasIndex(entity => entity.UserId).HasDatabaseName("ix_authentication_sessions_user_id");
        modelBuilder.Entity<ApiClientCompany>().HasIndex(entity => entity.CompanyId).HasDatabaseName("ix_api_client_companies_company_id");
        modelBuilder.Entity<ApiClientPermission>().HasIndex(entity => entity.PermissionId).HasDatabaseName("ix_api_client_permissions_permission_id");
        modelBuilder.Entity<CompanySetting>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<Branch>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<OrganizationalUnit>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<Warehouse>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<UserCompany>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<UserRole>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<ApiClientCompany>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        modelBuilder.Entity<AuditEvent>().HasQueryFilter(entity => companyContext.CompanyId != null && entity.CompanyId == companyContext.CompanyId);
        base.OnModelCreating(modelBuilder);
    }
}
