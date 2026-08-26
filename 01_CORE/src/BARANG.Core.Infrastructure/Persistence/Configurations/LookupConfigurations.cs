using BARANG.Core.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class LookupTypeConfiguration : IEntityTypeConfiguration<LookupType>
{
    public void Configure(EntityTypeBuilder<LookupType> b)
    {
        b.ToTable("lookup_types", t => { t.HasCheckConstraint("ck_lookup_types_version_positive", "version > 0"); t.HasCheckConstraint("ck_lookup_types_scope_company", "(scope = 'Global' AND company_id IS NULL) OR (scope = 'Company' AND company_id IS NOT NULL)"); });
        b.HasKey(x => x.Id).HasName("pk_lookup_types"); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedNever(); b.Property(x=>x.CompanyId).HasColumnName("company_id"); b.Property(x=>x.Code).HasColumnName("code").HasMaxLength(100); b.Property(x=>x.DisplayName).HasColumnName("display_name").HasMaxLength(200); b.Property(x=>x.Scope).HasColumnName("scope").HasConversion<string>().HasMaxLength(16); b.Property(x=>x.IsSystem).HasColumnName("is_system"); b.Property(x=>x.IsActive).HasColumnName("is_active"); b.Property(x=>x.AllowCustomValues).HasColumnName("allow_custom_values"); Versioned(b);
        b.HasOne<Domain.Organization.Company>().WithMany().HasForeignKey(x=>x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lookup_types_companies_company_id");
        b.HasIndex(x=>x.Code).IsUnique().HasDatabaseName("uq_lookup_types_code"); b.HasIndex(x=>new{x.CompanyId,x.IsActive}).HasDatabaseName("ix_lookup_types_company_id_is_active");
    }
    internal static void Versioned<T>(EntityTypeBuilder<T> b) where T:class { b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnName("created_at_utc"); b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnName("updated_at_utc"); b.Property<long>("Version").HasColumnName("version").IsConcurrencyToken(); }
}
internal sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> b)
    {
        b.ToTable("lookup_values",t=>t.HasCheckConstraint("ck_lookup_values_version_positive","version > 0")); b.HasKey(x=>x.Id).HasName("pk_lookup_values"); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedNever(); b.Property(x=>x.LookupTypeId).HasColumnName("lookup_type_id"); b.Property(x=>x.CompanyId).HasColumnName("company_id"); b.Property(x=>x.Code).HasColumnName("code").HasMaxLength(100); b.Property(x=>x.DisplayOrder).HasColumnName("display_order"); b.Property(x=>x.IsActive).HasColumnName("is_active"); b.Property(x=>x.IsSystem).HasColumnName("is_system"); LookupTypeConfiguration.Versioned(b);
        b.HasOne<LookupType>().WithMany().HasForeignKey(x=>x.LookupTypeId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lookup_values_lookup_types_lookup_type_id"); b.HasOne<Domain.Organization.Company>().WithMany().HasForeignKey(x=>x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lookup_values_companies_company_id"); b.HasMany(x=>x.Translations).WithOne().HasForeignKey(x=>x.LookupValueId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lookup_value_translations_lookup_values_lookup_value_id");
        b.HasIndex(x=>new{x.LookupTypeId,x.Code}).IsUnique().HasDatabaseName("uq_lookup_values_lookup_type_id_code"); b.HasIndex(x=>new{x.CompanyId,x.LookupTypeId,x.IsActive,x.DisplayOrder}).HasDatabaseName("ix_lookup_values_options");
        b.Navigation(x=>x.Translations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
internal sealed class LookupValueTranslationConfiguration : IEntityTypeConfiguration<LookupValueTranslation>
{
    public void Configure(EntityTypeBuilder<LookupValueTranslation> b)
    {
        b.ToTable("lookup_value_translations",t=>t.HasCheckConstraint("ck_lookup_value_translations_version_positive","version > 0")); b.HasKey(x=>x.Id).HasName("pk_lookup_value_translations"); b.Property(x=>x.Id).HasColumnName("id").ValueGeneratedNever(); b.Property(x=>x.LookupValueId).HasColumnName("lookup_value_id"); b.Property(x=>x.CompanyId).HasColumnName("company_id"); b.Property(x=>x.Culture).HasColumnName("culture").HasMaxLength(10); b.Property(x=>x.DisplayName).HasColumnName("display_name").HasMaxLength(200); b.Property(x=>x.Description).HasColumnName("description").HasMaxLength(1000); LookupTypeConfiguration.Versioned(b); b.HasOne<Domain.Organization.Company>().WithMany().HasForeignKey(x=>x.CompanyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_lookup_value_translations_companies_company_id"); b.HasIndex(x=>new{x.LookupValueId,x.Culture}).IsUnique().HasDatabaseName("uq_lookup_value_translations_value_culture"); b.HasIndex(x=>x.CompanyId).HasDatabaseName("ix_lookup_value_translations_company_id");
    }
}
