using BARANG.Core.Domain.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class SystemFeatureConfiguration : IEntityTypeConfiguration<SystemFeature>
{
    public void Configure(EntityTypeBuilder<SystemFeature> b)
    {
        b.ToTable("system_features", t =>
        {
            t.HasCheckConstraint("ck_system_features_scope_valid", "scope IN ('erp','store')");
            t.HasCheckConstraint("ck_system_features_version_positive", "version > 0");
        });
        b.HasKey(x => x.Id).HasName("pk_system_features");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.FeatureCode).HasColumnName("feature_code").HasMaxLength(64);
        b.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(8);
        b.Property(x => x.NameFa).HasColumnName("name_fa").HasMaxLength(200).IsRequired().HasDefaultValue("");
        b.Property(x => x.NameEn).HasColumnName("name_en").HasMaxLength(200).IsRequired().HasDefaultValue("");
        b.Property(x => x.NameAr).HasColumnName("name_ar").HasMaxLength(200).IsRequired().HasDefaultValue("");
        b.Property(x => x.NameEs).HasColumnName("name_es").HasMaxLength(200).IsRequired().HasDefaultValue("");
        b.Property(x => x.DescriptionFa).HasColumnName("description_fa").HasMaxLength(600).IsRequired().HasDefaultValue("");
        b.Property(x => x.DescriptionEn).HasColumnName("description_en").HasMaxLength(600).IsRequired().HasDefaultValue("");
        b.Property(x => x.DescriptionAr).HasColumnName("description_ar").HasMaxLength(600).IsRequired().HasDefaultValue("");
        b.Property(x => x.DescriptionEs).HasColumnName("description_es").HasMaxLength(600).IsRequired().HasDefaultValue("");
        b.Property(x => x.IsEnabled).HasColumnName("is_enabled");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_system_features_company_id");
        // یگانه روی (شرکت، کد فیچر، اسکوپ) — همان کد فیچر می‌تواند در هر دو اسکوپ erp/store وجود داشته باشد
        b.HasIndex(x => new { x.CompanyId, x.FeatureCode, x.Scope }).IsUnique().HasDatabaseName("uq_system_features_company_code_scope");
    }
}
