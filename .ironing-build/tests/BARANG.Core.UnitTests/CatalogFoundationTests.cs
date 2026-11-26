using BARANG.Core.Domain.Catalog;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class CatalogFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
    [Fact] public void ProductNormalizesStableTechnicalCodes() { Product value = Product.Create(Guid.NewGuid(), Guid.NewGuid(), " prd-1 ", " Shirt ", null, " apparel ", " pcs ", Now).Value; Assert.Equal("PRD-1", value.ProductCode); Assert.Equal("APPAREL", value.ProductTypeCode); Assert.Equal("PCS", value.UnitOfMeasureCode); }
    [Fact] public void ProductRejectsInvalidCode() => Assert.True(Product.Create(Guid.NewGuid(), Guid.NewGuid(), "localized code!", "Shirt", null, "APPAREL", "PCS", Now).IsFailure);
    [Fact] public void VariantNormalizesSkuBarcodeSizeAndColor() { Product p = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "P-1", "Shirt", null, "APPAREL", "PCS", Now).Value; ProductVariant v = ProductVariant.Create(Guid.NewGuid(), p, " sku-1 ", null, " ab-12 ", " xl ", " black ", Now).Value; Assert.Equal("SKU-1", v.Sku); Assert.Equal("AB-12", v.Barcode); Assert.Equal("XL", v.SizeCode); Assert.Equal("BLACK", v.ColorCode); }
    [Fact] public void ProductInactivityDoesNotDestroyVariantLifecycle() { Product p = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "P-1", "Shirt", null, "APPAREL", "PCS", Now).Value; ProductVariant v = ProductVariant.Create(Guid.NewGuid(), p, "SKU-1", null, null, null, null, Now).Value; p.SetActive(false, Now.AddMinutes(1)); Assert.True(v.IsActive); Assert.False(p.IsActive); }
    [Fact] public void SupplierReusesCustomerContactNormalization() { Supplier s = Supplier.Create(Guid.NewGuid(), Guid.NewGuid(), " sup-1 ", "Supplier", null, null, null, " SALES@EXAMPLE.COM ", null, "+44 (7700) 900-123", null, null, Now).Value; Assert.Equal("SUP-1", s.SupplierCode); Assert.Equal("sales@example.com", s.PrimaryEmail); Assert.Equal("+447700900123", s.PrimaryMobile); }
    [Theory] [InlineData(-1, 2)] [InlineData(1, -2)] public void SupplierRelationshipRejectsNegativePlanningHints(decimal moq, int lead) => Assert.True(SupplierProduct.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null, false, moq, lead, Now).IsFailure);
    [Fact] public void EquipmentSeparatesLifecycleAndOperationalStatus() { Equipment e = Equipment.Create(Guid.NewGuid(), Guid.NewGuid(), " eq-1 ", "Press", " heat.press ", " maintenance ", null, null, null, null, null, null, Now).Value; e.SetActive(false, Now.AddMinutes(1)); Assert.False(e.IsActive); Assert.Equal("MAINTENANCE", e.OperationalStatusCode); }
    [Fact] public void UpdatesAdvanceConcurrencyVersion() { Product p = Product.Create(Guid.NewGuid(), Guid.NewGuid(), "P-1", "Shirt", null, "APPAREL", "PCS", Now).Value; Assert.True(p.Update("New Shirt", null, "APPAREL", "EA", Now.AddMinutes(1)).IsSuccess); Assert.Equal(2, p.Version); }
    [Fact] public void DomainLengthsMatchPostgreSqlCatalogColumns()
    {
        Guid company = Guid.NewGuid();
        Assert.True(Product.Create(Guid.NewGuid(), company, new string('P', 50), "Product", null, new string('T', 50), new string('U', 30), Now).IsSuccess);
        Assert.True(Product.Create(Guid.NewGuid(), company, new string('P', 51), "Product", null, "TYPE", "UOM", Now).IsFailure);
        Assert.True(Product.Create(Guid.NewGuid(), company, "P", "Product", null, new string('T', 51), "UOM", Now).IsFailure);
        Assert.True(Product.Create(Guid.NewGuid(), company, "P", "Product", null, "TYPE", new string('U', 31), Now).IsFailure);

        Product product = Product.Create(Guid.NewGuid(), company, "P", "Product", null, "TYPE", "UOM", Now).Value;
        Assert.True(ProductVariant.Create(Guid.NewGuid(), product, new string('S', 80), null, new string('B', 100), new string('Z', 50), new string('C', 50), Now).IsSuccess);
        Assert.True(ProductVariant.Create(Guid.NewGuid(), product, new string('S', 81), null, null, null, null, Now).IsFailure);
        Assert.True(ProductVariant.Create(Guid.NewGuid(), product, "SKU", null, new string('B', 101), null, null, Now).IsFailure);
        Assert.True(ProductVariant.Create(Guid.NewGuid(), product, "SKU", null, null, new string('Z', 51), null, Now).IsFailure);
        Assert.True(ProductVariant.Create(Guid.NewGuid(), product, "SKU", null, null, null, new string('C', 51), Now).IsFailure);

        Assert.True(Supplier.Create(Guid.NewGuid(), company, new string('S', 51), "Supplier", null, null, null, null, null, null, null, null, Now).IsFailure);
        SupplierProduct link = SupplierProduct.Create(Guid.NewGuid(), company, Guid.NewGuid(), product.Id, null, new string('I', 80), false, null, null, Now).Value;
        Assert.True(link.Update(new string('I', 81), false, null, null, Now).IsFailure);

        Assert.True(Equipment.Create(Guid.NewGuid(), company, new string('E', 51), "Equipment", "TYPE", "STATUS", null, null, null, null, null, null, Now).IsFailure);
        Assert.True(Equipment.Create(Guid.NewGuid(), company, "E", "Equipment", new string('T', 51), "STATUS", null, null, null, null, null, null, Now).IsFailure);
        Assert.True(Equipment.Create(Guid.NewGuid(), company, "E", "Equipment", "TYPE", new string('S', 51), null, null, null, null, null, null, Now).IsFailure);
        Assert.True(Equipment.Create(Guid.NewGuid(), company, "E", "Equipment", "TYPE", "STATUS", null, null, null, new string('M', 151), null, null, Now).IsFailure);
    }
}
