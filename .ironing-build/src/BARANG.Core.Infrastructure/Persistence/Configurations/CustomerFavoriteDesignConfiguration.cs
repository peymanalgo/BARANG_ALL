using BARANG.Core.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class CustomerFavoriteDesignConfiguration : IEntityTypeConfiguration<CustomerFavoriteDesign>
{
    public void Configure(EntityTypeBuilder<CustomerFavoriteDesign> b)
    {
        b.ToTable("customer_favorite_designs");
        b.HasKey(x => x.Id).HasName("pk_customer_favorite_designs");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ArtworkId).HasColumnName("artwork_id");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.HasIndex(x => new { x.CustomerId, x.ArtworkId }).IsUnique().HasDatabaseName("uq_customer_favorite_designs_customer_artwork");
        b.HasIndex(x => x.CompanyId).HasDatabaseName("ix_customer_favorite_designs_company_id");
    }
}
