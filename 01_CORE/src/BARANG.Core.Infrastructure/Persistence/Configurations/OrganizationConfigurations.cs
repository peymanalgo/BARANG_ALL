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
internal sealed class OrganizationalAssignmentConfiguration : IEntityTypeConfiguration<OrganizationalAssignment>
{
    public void Configure(EntityTypeBuilder<OrganizationalAssignment> b)
    {
        b.ToTable("organizational_assignments",t=>{t.HasCheckConstraint("ck_organizational_assignments_version_positive","version > 0");t.HasCheckConstraint("ck_organizational_assignments_dates","end_date IS NULL OR start_date IS NULL OR end_date >= start_date");t.HasCheckConstraint("ck_organizational_assignments_not_own_manager","manager_assignment_id IS NULL OR manager_assignment_id <> id");}); b.HasKey(x=>x.Id).HasName("pk_organizational_assignments"); b.Property(x=>x.Id).HasColumnName("id"); b.Property(x=>x.CompanyId).HasColumnName("company_id"); b.Property(x=>x.PersonId).HasColumnName("person_id"); b.Property(x=>x.OrganizationalUnitId).HasColumnName("organizational_unit_id"); b.Property(x=>x.ManagerAssignmentId).HasColumnName("manager_assignment_id"); b.Property(x=>x.PositionCode).HasColumnName("position_code").HasMaxLength(64); b.Property(x=>x.PositionTitle).HasColumnName("position_title").HasMaxLength(200); b.Property(x=>x.IsPrimary).HasColumnName("is_primary"); b.Property(x=>x.StartDate).HasColumnName("start_date"); b.Property(x=>x.EndDate).HasColumnName("end_date"); CompanyConfiguration.Metadata(b); b.HasOne<Person>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.PersonId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict); b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.OrganizationalUnitId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict); b.HasOne<OrganizationalAssignment>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.ManagerAssignmentId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict); b.HasAlternateKey(x=>new{x.CompanyId,x.Id}); b.HasIndex(x=>new{x.CompanyId,x.OrganizationalUnitId}); b.HasIndex(x=>new{x.CompanyId,x.PersonId,x.OrganizationalUnitId}).HasFilter("is_active").IsUnique().HasDatabaseName("ux_organizational_assignments_active_placement"); b.HasIndex(x=>new{x.CompanyId,x.ManagerAssignmentId});
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);
    }
}

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> b)
    {
        b.ToTable("positions", t => {
            t.HasCheckConstraint("ck_positions_version_positive", "version > 0");
            t.HasCheckConstraint("ck_positions_not_own_manager", "reports_to_position_id IS NULL OR reports_to_position_id <> id");
        });
        b.HasKey(x => x.Id).HasName("pk_positions");
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.OrganizationalUnitId).HasColumnName("organizational_unit_id");
        b.Property(x => x.ReportsToPositionId).HasColumnName("reports_to_position_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x => x.SemanticRole).HasColumnName("semantic_role").HasMaxLength(64);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_positions_companies_company_id");
        b.HasOne<OrganizationalUnit>().WithMany().HasForeignKey(x => new { x.CompanyId, x.OrganizationalUnitId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_positions_units_company_id_unit_id");
        b.HasOne<Position>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ReportsToPositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_positions_reports_to_company_id_position_id");
        b.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_positions_company_id_id");
        b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("uq_positions_company_id_code");
        b.HasIndex(x => new { x.CompanyId, x.OrganizationalUnitId, x.IsActive }).HasDatabaseName("ix_positions_company_unit_active");
    }
}

internal sealed class PositionAssignmentConfiguration : IEntityTypeConfiguration<PositionAssignment>
{
    public void Configure(EntityTypeBuilder<PositionAssignment> b)
    {
        b.ToTable("position_assignments", t => {
            t.HasCheckConstraint("ck_position_assignments_version_positive", "version > 0");
            t.HasCheckConstraint("ck_position_assignments_dates", "end_date IS NULL OR start_date IS NULL OR end_date >= start_date");
        });
        b.HasKey(x => x.Id).HasName("pk_position_assignments");
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.PositionId).HasColumnName("position_id"); b.Property(x => x.PersonId).HasColumnName("person_id");
        b.Property(x => x.StartDate).HasColumnName("start_date"); b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.IsPrimary).HasColumnName("is_primary"); b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc"); b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc"); b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_position_assignments_companies_company_id");
        b.HasOne<Position>().WithMany().HasForeignKey(x => new { x.CompanyId, x.PositionId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_position_assignments_positions_company_id_position_id");
        b.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CompanyId, x.PersonId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_position_assignments_persons_company_id_person_id");
        b.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_position_assignments_company_id_id");
        b.HasIndex(x => new { x.CompanyId, x.PositionId, x.Status }).HasDatabaseName("ix_position_assignments_position_status");
        b.HasIndex(x => new { x.CompanyId, x.PersonId, x.Status }).HasDatabaseName("ix_position_assignments_person_status");
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

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> b)
    {
        b.ToTable("persons", t => t.HasCheckConstraint("ck_persons_version_positive", "version > 0"));
        b.HasKey(x => x.Id).HasName("pk_persons");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.PersonnelCode).HasColumnName("personnel_code").HasMaxLength(64);
        b.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        b.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        b.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(201);
        b.Property(x => x.NationalId).HasColumnName("national_id").HasMaxLength(64);
        b.Property(x => x.BirthDate).HasColumnName("birth_date");
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
        b.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(64);
        b.Property(x => x.Address).HasColumnName("address").HasMaxLength(1000);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        CompanyConfiguration.Metadata(b);
        b.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_persons_companies_company_id");
        b.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("uq_persons_company_id_id");
        b.HasIndex(x => new { x.CompanyId, x.PersonnelCode }).IsUnique().HasDatabaseName("uq_persons_company_id_personnel_code");
        b.HasIndex(x => new { x.CompanyId, x.DisplayName }).HasDatabaseName("ix_persons_company_id_display_name");
    }
}
