using BARANG.Core.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class DesignAuditRuleConfiguration : IEntityTypeConfiguration<DesignAuditRule>
{
    public void Configure(EntityTypeBuilder<DesignAuditRule> b)
    {
        b.ToTable("design_audit_rules", "barang");
        b.HasKey(x => x.Id).HasName("pk_design_audit_rules");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(60);
        b.Property(x => x.RuleType).HasColumnName("rule_type").HasMaxLength(40);
        b.Property(x => x.TitleFa).HasColumnName("title_fa").HasMaxLength(200);
        b.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(200);
        b.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(200);
        b.Property(x => x.TitleEs).HasColumnName("title_es").HasMaxLength(200);
        b.Property(x => x.ParamsJson).HasColumnName("params").HasColumnType("jsonb").HasMaxLength(2000);
        b.Property(x => x.PrintSizeCode).HasColumnName("print_size_code").HasMaxLength(10);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_design_audit_rules_company_id");
        b.HasIndex(x => new { x.CompanyId, x.Code }).IsUnique().HasDatabaseName("uq_design_audit_rules_company_code");
    }
}
