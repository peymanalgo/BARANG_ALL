using BARANG.Core.Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class ArtworkTranslationConfiguration : IEntityTypeConfiguration<ArtworkTranslation>
{
    public void Configure(EntityTypeBuilder<ArtworkTranslation> b)
    {
        b.ToTable("artwork_translations");
        b.HasKey(x => x.Id).HasName("pk_artwork_translations");
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.ArtworkId).HasColumnName("artwork_id");
        b.Property(x => x.Culture).HasColumnName("culture").HasMaxLength(10).IsRequired();
        b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version");
        b.HasIndex(x => new { x.ArtworkId, x.Culture }).IsUnique().HasDatabaseName("uq_artwork_translations_artwork_culture");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_artwork_translations_company_id");
    }
}
