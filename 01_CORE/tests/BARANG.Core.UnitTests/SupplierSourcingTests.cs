using BARANG.Core.Domain.Catalog;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class SupplierSourcingTests
{
    private static readonly DateTimeOffset Now=new(2026,9,13,12,0,0,TimeSpan.Zero);
    private static SupplierPriceData Price(decimal amount=12.3456m)=>new(Guid.NewGuid(),"usd",new(2026,9,1),new(2026,9,30),new(Guid.NewGuid(),amount));
    [Theory][InlineData(-1)][InlineData(1.00001)][InlineData(100000000000000)]
    public void InvalidPrecisionOrNegativePurchasePricesAreRejected(decimal amount)=>
        Assert.True(SupplierProductPrice.Create(Guid.NewGuid(),Guid.NewGuid(),Price(amount),Now).IsFailure);
    [Fact]public void DraftPriceKeepsFourDecimalPrecisionAndCanBeEdited(){
        var data=Price();var price=SupplierProductPrice.Create(Guid.NewGuid(),Guid.NewGuid(),data,Now).Value;
        Assert.Equal(12.3456m,price.UnitPrice);Assert.Equal("USD",price.CurrencyCode);Assert.Equal("Draft",price.Status);
        Assert.True(price.Update(data with{Terms=data.Terms with{UnitPrice=9.8765m}},Now).IsSuccess);
        Assert.Equal(9.8765m,price.UnitPrice);Assert.Equal(2,price.Version);
    }
    [Fact]public void PublishedAmountIsImmutableButDeactivationAndProspectiveEndDatePreserveHistory(){
        var data=Price();var price=SupplierProductPrice.Create(Guid.NewGuid(),Guid.NewGuid(),data,Now).Value;price.Publish(true,Now);
        Assert.Equal("supplier.sourcing.published_immutable",price.Update(data with{Terms=data.Terms with{UnitPrice=99}},Now).Error.Code);
        Assert.Equal(12.3456m,price.UnitPrice);Assert.False(price.End(new(2026,9,12),Now).IsSuccess);
        Assert.True(price.End(new(2026,9,20),Now).IsSuccess);price.Publish(false,Now);
        Assert.Equal("Inactive",price.Status);Assert.NotNull(price.PublishedAtUtc);Assert.Equal(12.3456m,price.UnitPrice);
    }
    [Theory][InlineData(0,0,0,100)][InlineData(10,10,2,101)][InlineData(100,20,3,3)]
    public void PurchaseCostUsesDecimalDiscountTaxAndPerUnitFreight(decimal discount,decimal tax,decimal freight,decimal expected)=>
        Assert.Equal(expected,SupplierSourcingRules.NetUnitCost(new(Guid.NewGuid(),100,DiscountPercent:discount,TaxRatePercent:tax,FreightPerUnit:freight)));
    [Fact]public void EffectiveWindowsAreInclusiveAndAdjacentDaysDoNotOverlap(){
        Assert.True(SupplierSourcingRules.Overlaps(new(2026,1,1),new(2026,1,31),new(2026,1,31),null));
        Assert.False(SupplierSourcingRules.Overlaps(new(2026,1,1),new(2026,1,31),new(2026,2,1),null));
        Assert.True(SupplierSourcingRules.Applies(new(2026,1,1),null,new(2030,1,1)));
    }
    [Fact]public void PriceListHeaderAndLinesFreezeAfterPublication(){
        var data=new SupplierPriceListData("PRICE-1","Supplier quote","IRR",new(2026,9,1));
        var header=SupplierPriceList.Create(Guid.NewGuid(),Guid.NewGuid(),data,Now).Value;
        var lineData=new SupplierPriceListLineData(Guid.NewGuid(),new(Guid.NewGuid(),12));
        var line=SupplierPriceListLine.Create(header,lineData,Now).Value;
        header.Publish(true,Now);line.BindPublishedPrice(Guid.NewGuid(),Now);
        Assert.True(header.Update(data with{Title="Changed"},Now).IsFailure);
        Assert.True(line.Update(lineData with{Terms=lineData.Terms with{UnitPrice=99}},Now).IsFailure);
        Assert.True(SupplierPriceListLine.Create(header,lineData,Now).IsFailure);
    }
    [Fact]public void SourcingRelationshipOwnsHintsNotProductOrLotAttributes(){
        var data=new SupplierSourcingData(Guid.NewGuid(),PurchaseUnitId:Guid.NewGuid(),SupplierItemCode:"local-1",MinimumOrderQuantity:1.2345m,LeadTimeDays:7);
        var row=SupplierProduct.CreateSourcing(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),data,Now).Value;
        Assert.Equal("LOCAL-1",row.SupplierItemCode);Assert.Equal(1.2345m,row.MinimumOrderQuantity);
        Assert.True(row.UpdateSourcing(data with{ProductId=Guid.NewGuid()},Now).IsFailure);
        Assert.True(row.UpdateSourcing(data with{LeadTimeDays=-1},Now).IsFailure);
        Assert.Null(typeof(SupplierProduct).GetProperty("BatchNumber"));Assert.Null(typeof(SupplierProduct).GetProperty("OnHand"));
    }
}
