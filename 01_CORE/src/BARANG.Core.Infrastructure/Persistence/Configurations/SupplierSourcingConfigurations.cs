using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal static class SupplierSourcingConfiguration
{
    internal static void Relationship(EntityTypeBuilder<SupplierProduct> b)
    {
        b.Property(x=>x.PurchaseUnitId).HasColumnName("purchase_unit_id");
        b.Property(x=>x.PackagingTypeId).HasColumnName("packaging_type_id");
        b.Property(x=>x.SupplierItemName).HasColumnName("supplier_item_name").HasMaxLength(200);
        b.Property(x=>x.Description).HasColumnName("description").HasMaxLength(2000);
        b.Property(x=>x.Brand).HasColumnName("brand").HasMaxLength(150);
        b.Property(x=>x.MinimumOrderAmount).HasColumnName("minimum_order_amount").HasPrecision(18,4);
        b.Property(x=>x.Availability).HasColumnName("availability").HasMaxLength(20).HasDefaultValue("Available");
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PackagingTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).HasName("ak_supplier_products_company_supplier_id");
        b.HasIndex(x=>new{x.CompanyId,x.ProductId}).IsUnique().HasFilter("product_variant_id IS NULL AND is_preferred AND is_active").HasDatabaseName("uq_supplier_products_preferred_product");
        b.HasIndex(x=>new{x.CompanyId,x.ProductVariantId}).IsUnique().HasFilter("product_variant_id IS NOT NULL AND is_preferred AND is_active").HasDatabaseName("uq_supplier_products_preferred_variant");
        b.ToTable("supplier_products",t=>{
            t.HasCheckConstraint("ck_supplier_products_sourcing_amount","minimum_order_amount IS NULL OR minimum_order_amount >= 0");
            t.HasCheckConstraint("ck_supplier_products_availability","availability IN ('Available','OnRequest','Unavailable')");
        });
    }
    internal const string TermCheck="unit_price >= 0 AND minimum_quantity >= 0 AND discount_percent BETWEEN 0 AND 100 AND tax_rate_percent BETWEEN 0 AND 100 AND freight_per_unit >= 0";
    internal const string PeriodCheck="currency_code ~ '^[A-Z]{3}$' AND (effective_to IS NULL OR effective_to >= effective_from) AND (NOT is_active OR published_at_utc IS NOT NULL)";
}
internal sealed class SupplierProductPriceConfiguration:IEntityTypeConfiguration<SupplierProductPrice>
{
    public void Configure(EntityTypeBuilder<SupplierProductPrice> b)
    {
        CatalogConfiguration.Base(b,"supplier_product_prices");
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.SupplierProductId).HasColumnName("supplier_product_id");
        b.HasOne<SupplierProduct>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId}).HasPrincipalKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        b.Property(x=>x.EffectiveFrom).HasColumnName("effective_from");
        b.Property(x=>x.EffectiveTo).HasColumnName("effective_to");
        b.Property(x=>x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Ignore(x=>x.Status);
        b.Property(x=>x.PriceListId).HasColumnName("price_list_id");
        b.HasOne<SupplierPriceList>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId,x.PriceListId}).HasPrincipalKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasAlternateKey(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId,x.Id}).HasName("ak_supplier_prices_company_supplier_product_id");
        b.Property(x=>x.PurchaseUnitId).HasColumnName("purchase_unit_id");
        b.Property(x=>x.UnitPrice).HasColumnName("unit_price").HasPrecision(18,4);
        b.Property(x=>x.MinimumQuantity).HasColumnName("minimum_quantity").HasPrecision(18,4);
        b.Property(x=>x.DiscountPercent).HasColumnName("discount_percent").HasPrecision(18,4);
        b.Property(x=>x.TaxStatusId).HasColumnName("tax_status_id");
        b.Property(x=>x.TaxRatePercent).HasColumnName("tax_rate_percent").HasPrecision(18,4);
        b.Property(x=>x.FreightPerUnit).HasColumnName("freight_per_unit").HasPrecision(18,4);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.Ignore(x=>x.Terms);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.TaxStatusId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId,x.EffectiveFrom}).HasDatabaseName("ix_supplier_prices_history");
        b.HasIndex(x=>new{x.CompanyId,x.SupplierProductId,x.PurchaseUnitId,x.CurrencyCode,x.MinimumQuantity}).HasDatabaseName("ix_supplier_prices_resolution");
        b.ToTable("supplier_product_prices",t=>{
            t.HasCheckConstraint("ck_supplier_prices_terms",SupplierSourcingConfiguration.TermCheck);
            t.HasCheckConstraint("ck_supplier_prices_period",SupplierSourcingConfiguration.PeriodCheck);
        });
    }
}
internal sealed class SupplierPriceListConfiguration:IEntityTypeConfiguration<SupplierPriceList>
{
    public void Configure(EntityTypeBuilder<SupplierPriceList> b)
    {
        CatalogConfiguration.Base(b,"supplier_price_lists");
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        b.Property(x=>x.EffectiveFrom).HasColumnName("effective_from");
        b.Property(x=>x.EffectiveTo).HasColumnName("effective_to");
        b.Property(x=>x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Ignore(x=>x.Status);
        b.Property(x=>x.Code).HasColumnName("code").HasMaxLength(50);
        b.Property(x=>x.Title).HasColumnName("title").HasMaxLength(200);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.HasAlternateKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).HasName("ak_supplier_price_lists_company_supplier_id");
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.Code}).IsUnique().HasDatabaseName("uq_supplier_price_lists_code");
        b.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.EffectiveFrom}).HasDatabaseName("ix_supplier_price_lists_directory");
        b.ToTable("supplier_price_lists",t=>t.HasCheckConstraint("ck_supplier_price_lists_period",SupplierSourcingConfiguration.PeriodCheck));
    }
}
internal sealed class SupplierPriceListLineConfiguration:IEntityTypeConfiguration<SupplierPriceListLine>
{
    public void Configure(EntityTypeBuilder<SupplierPriceListLine> b)
    {
        CatalogConfiguration.Base(b,"supplier_price_list_lines");
        b.Property(x=>x.SupplierId).HasColumnName("supplier_id");
        b.HasOne<Supplier>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.SupplierProductId).HasColumnName("supplier_product_id");
        b.HasOne<SupplierProduct>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId}).HasPrincipalKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.PriceListId).HasColumnName("price_list_id");
        b.Property(x=>x.PublishedPriceId).HasColumnName("published_price_id");
        b.HasOne<SupplierPriceList>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId,x.PriceListId}).HasPrincipalKey(x=>new{x.CompanyId,x.SupplierId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SupplierProductPrice>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId,x.PublishedPriceId}).HasPrincipalKey(x=>new{x.CompanyId,x.SupplierId,x.SupplierProductId,x.Id}).OnDelete(DeleteBehavior.Restrict);
        b.Property(x=>x.PurchaseUnitId).HasColumnName("purchase_unit_id");
        b.Property(x=>x.UnitPrice).HasColumnName("unit_price").HasPrecision(18,4);
        b.Property(x=>x.MinimumQuantity).HasColumnName("minimum_quantity").HasPrecision(18,4);
        b.Property(x=>x.DiscountPercent).HasColumnName("discount_percent").HasPrecision(18,4);
        b.Property(x=>x.TaxStatusId).HasColumnName("tax_status_id");
        b.Property(x=>x.TaxRatePercent).HasColumnName("tax_rate_percent").HasPrecision(18,4);
        b.Property(x=>x.FreightPerUnit).HasColumnName("freight_per_unit").HasPrecision(18,4);
        b.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(2000);
        b.Ignore(x=>x.Terms);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.PurchaseUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LookupValue>().WithMany().HasForeignKey(x=>x.TaxStatusId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.CompanyId,x.PriceListId,x.SupplierProductId,x.PurchaseUnitId,x.MinimumQuantity}).IsUnique().HasDatabaseName("uq_supplier_price_list_lines_break");
        b.ToTable("supplier_price_list_lines",t=>t.HasCheckConstraint("ck_supplier_price_list_lines_terms",SupplierSourcingConfiguration.TermCheck));
    }
}
