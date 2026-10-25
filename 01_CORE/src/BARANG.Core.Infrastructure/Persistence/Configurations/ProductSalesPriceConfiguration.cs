using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class ProductSalesPriceConfiguration : IEntityTypeConfiguration<ProductSalesPrice>
{
    public void Configure(EntityTypeBuilder<ProductSalesPrice> b)
    {
        CatalogConfiguration.Base(b, "product_sales_prices");
        b.Property(x => x.ProductId).HasColumnName("product_id");
        b.Property(x => x.ProductVariantId).HasColumnName("product_variant_id");
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength();
        b.Property(x => x.StartDate).HasColumnName("start_date");
        b.Property(x => x.EndDate).HasColumnName("end_date");
        b.Property(x => x.MigratedFromTariffId).HasColumnName("migrated_from_tariff_id");
        b.HasOne<Product>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ProductId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ProductId, x.ProductVariantId })
            .HasPrincipalKey(x => new { x.CompanyId, x.ProductId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.ProductId, x.ProductVariantId, x.StartDate })
            .HasDatabaseName("ix_product_sales_prices_resolution");
        b.ToTable("product_sales_prices", t =>
        {
            t.HasCheckConstraint("ck_product_sales_prices_amount", "amount >= 0");
            t.HasCheckConstraint("ck_product_sales_prices_period", "end_date IS NULL OR end_date >= start_date");
        });
    }
}
