using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("companies"); b.HasKey(x => x.Id).HasName("pk_companies");
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64); b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        Metadata(b); b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_companies_code"); b.ToTable(t => t.HasCheckConstraint("ck_companies_version_positive", "version > 0"));
    }
    internal static void Metadata<TEntity>(EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        b.Property("IsActive").HasColumnName("is_active"); b.Property("CreatedAtUtc").HasColumnName("created_at_utc"); b.Property("UpdatedAtUtc").HasColumnName("updated_at_utc"); b.Property("Version").HasColumnName("version").IsConcurrencyToken();
    }
}

internal sealed class CompanySettingConfiguration : IEntityTypeConfiguration<CompanySetting>
{
    public void Configure(EntityTypeBuilder<CompanySetting> b)
    {
        b.ToTable("company_settings"); b.HasKey(x => x.Id).HasName("pk_company_settings"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.Key).HasColumnName("key").HasMaxLength(100); b.Property(x => x.Value).HasColumnName("value").HasMaxLength(2000); b.Property(x => x.ValueType).HasColumnName("value_type").HasConversion<string>().HasMaxLength(32); CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_company_settings_companies_company_id"); b.HasIndex(x => new { x.CompanyId, x.Key }).IsUnique().HasDatabaseName("uq_company_settings_company_id_key"); b.ToTable(t => t.HasCheckConstraint("ck_company_settings_version_positive", "version > 0"));
    }
}

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.ToTable("branches"); b.HasKey(x => x.Id).HasName("pk_branches"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64); b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200); CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_branches_companies_company_id"); b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("uq_branches_company_id_code"); b.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_branches_company_id_id"); b.ToTable(t => t.HasCheckConstraint("ck_branches_version_positive", "version > 0"));
    }
}

internal sealed class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> b)
    {
        b.ToTable("organizational_units"); b.HasKey(x => x.Id).HasName("pk_organizational_units"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.BranchId).HasColumnName("branch_id"); b.Property(x => x.ParentId).HasColumnName("parent_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64); b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200); CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_organizational_units_companies_company_id"); b.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.CompanyId, x.BranchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_organizational_units_branches_company_id_branch_id"); b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ParentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_organizational_units_parent_company_id_parent_id"); b.HasIndex(x => new { x.CompanyId, x.BranchId }).HasDatabaseName("ix_organizational_units_company_id_branch_id"); b.HasIndex(x => new { x.CompanyId, x.ParentId }).HasDatabaseName("ix_organizational_units_company_id_parent_id"); b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("uq_organizational_units_company_id_code"); b.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_organizational_units_company_id_id"); b.ToTable(t => { t.HasCheckConstraint("ck_organizational_units_not_own_parent", "parent_id IS NULL OR parent_id <> id"); t.HasCheckConstraint("ck_organizational_units_version_positive", "version > 0"); });
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.ToTable("warehouses"); b.HasKey(x => x.Id).HasName("pk_warehouses"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id"); b.Property(x => x.BranchId).HasColumnName("branch_id"); b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64); b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200); b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(32); CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_warehouses_companies_company_id"); b.HasOne<Branch>().WithMany().HasForeignKey(x => new { x.CompanyId, x.BranchId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_warehouses_branches_company_id_branch_id"); b.HasIndex(x => new { x.CompanyId, x.BranchId }).HasDatabaseName("ix_warehouses_company_id_branch_id"); b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("uq_warehouses_company_id_code"); b.ToTable(t => t.HasCheckConstraint("ck_warehouses_version_positive", "version > 0"));
    }
}
