using BARANG.Core.Domain.BusinessAnalysis;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class BusinessProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CustomerProfileNormalizesBusinessIdentityAndEnforcesFinancialRules()
    {
        CustomerBusinessProfile profile = CustomerBusinessProfile.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(1990, 1, 2), " ec-100 ", 500_000m, 30, "Wholesale", Now).Value;
        Assert.Equal("EC-100", profile.EconomicCode);
        Assert.Equal(500_000m, profile.CreditLimit);
        Assert.Equal(30, profile.PaymentTermsDays);
        Assert.True(CustomerBusinessProfile.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, -1, 0, null, Now).IsFailure);
        Assert.True(CustomerBusinessProfile.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2027, 1, 1), null, 0, 0, null, Now).IsFailure);
    }

    [Fact]
    public void ProductProfileIsNormalizedAndConcurrencyProtected()
    {
        ProductTechnicalProfile profile = ProductTechnicalProfile.Create(Guid.NewGuid(), Guid.NewGuid(), null, " acme ", "cotton", "unisex", "regular", "crew", "180 gsm", Now).Value;
        Assert.Equal("ACME", profile.BrandCode);
        Assert.Equal("COTTON", profile.MaterialCode);
        Assert.True(profile.Update("OTHER", null, null, null, null, null, 99, Now).IsFailure);
        Assert.True(profile.Update("OTHER", null, null, null, null, null, 1, Now).IsSuccess);
        Assert.Equal(2, profile.Version);
    }

    [Fact]
    public void OrderAndPrintProfilesValidateCodesCountsAndStaleUpdates()
    {
        OrderBusinessProfile order = OrderBusinessProfile.Create(Guid.NewGuid(), Guid.NewGuid(), " courier ", "standard", true, "walk_in", "Counter order", Now).Value;
        Assert.Equal("COURIER", order.ShippingMethodCode);
        Assert.True(order.Update("POST", null, false, null, null, 7, Now).IsFailure);
        Assert.True(PrintTechnicalProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "portrait", "custom", 0, null, null, null, Now).IsFailure);
        PrintTechnicalProfile print = PrintTechnicalProfile.Create(Guid.NewGuid(), Guid.NewGuid(), "portrait", "custom", 2, null, null, "White underbase", Now).Value;
        Assert.Equal("PORTRAIT", print.DirectionCode);
        Assert.Equal(2, print.PrintCount);
    }

    [Fact]
    public void SubmittedItemSnapshotPrefersVariantSpecificValues()
    {
        Guid company = Guid.NewGuid(), productId = Guid.NewGuid(), variantId = Guid.NewGuid();
        ProductTechnicalProfile product = ProductTechnicalProfile.Create(company, productId, null, "BASE", "COTTON", "UNISEX", "REGULAR", "TEE", "base", Now).Value;
        ProductTechnicalProfile variant = ProductTechnicalProfile.Create(company, productId, variantId, null, "POLY", null, "SLIM", null, "variant", Now).Value;
        OrderItemTechnicalSnapshot snapshot = OrderItemTechnicalSnapshot.Create(company, Guid.NewGuid(), product, variant, Now);
        Assert.Equal("BASE", snapshot.BrandCode);
        Assert.Equal("POLY", snapshot.MaterialCode);
        Assert.Equal("SLIM", snapshot.FitCode);
        Assert.Equal("variant", snapshot.TechnicalAttributes);
    }
}
