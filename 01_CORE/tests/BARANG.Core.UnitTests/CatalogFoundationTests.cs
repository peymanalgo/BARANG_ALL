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
}
