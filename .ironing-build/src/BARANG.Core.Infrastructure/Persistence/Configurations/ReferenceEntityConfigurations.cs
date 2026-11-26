using BARANG.Core.Domain.Files;
using BARANG.Core.Domain.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BARANG.Core.Infrastructure.Persistence.Configurations;

// Localization metadata lives on the original aggregate rows; their IDs and foreign keys remain authoritative.
internal static class ReferenceEntityMetadata
{
    internal static void Configure<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property<string>("ReferenceNames").HasColumnName("reference_names").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property<string>("ReferenceDescriptions").HasColumnName("reference_descriptions").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property<int>("ReferenceDisplayOrder").HasColumnName("reference_display_order").HasDefaultValue(0);
    }
}
internal sealed class ArtworkTagReferenceConfiguration : IEntityTypeConfiguration<ArtworkTag>
{
    public void Configure(EntityTypeBuilder<ArtworkTag> builder) => ReferenceEntityMetadata.Configure(builder);
}
internal sealed class DefectTypeReferenceConfiguration : IEntityTypeConfiguration<DefectType>
{
    public void Configure(EntityTypeBuilder<DefectType> builder) => ReferenceEntityMetadata.Configure(builder);
}
