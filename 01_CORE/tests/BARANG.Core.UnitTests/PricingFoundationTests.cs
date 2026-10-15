using BARANG.Core.Domain.Orders;
using BARANG.Core.Domain.Pricing;
using Xunit;

namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707,CA1305
public sealed class PricingFoundationTests
{
    private static readonly Guid Company=Guid.NewGuid();private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-08-31T12:00:00Z");
    [Fact]public void Money_rounds_midpoints_away_from_zero()=>Assert.Equal(10.01m,Money.Create(10.005m,"irr").Value.Amount);
    [Fact]public void Money_requires_non_negative_amount_and_iso_style_currency(){Assert.True(Money.Create(-1,"IRR").IsFailure);Assert.True(Money.Create(1,"XX").IsFailure);}
    [Fact]public void Raw_product_tariff_requires_variant(){Assert.True(Tariff.Create(Company,TariffType.RawProduct,"RAW",null,null,null,10,"IRR",new(2026,1,1),null,Now).IsFailure);Assert.True(Tariff.Create(Company,TariffType.RawProduct,"RAW",Guid.NewGuid(),null,null,10,"IRR",new(2026,1,1),null,Now).IsSuccess);}
    [Fact]public void Tariff_rejects_invalid_period_and_negative_amount(){Assert.True(Tariff.Create(Company,TariffType.Shipping,"DEFAULT",null,null,null,-1,"IRR",new(2026,2,1),new(2026,1,1),Now).IsFailure);}
    [Fact]public void Tariff_update_enforces_concurrency(){Tariff x=Tariff.Create(Company,TariffType.Packaging,"DEFAULT",null,null,null,2,"IRR",new(2026,1,1),null,Now).Value;Assert.Equal(PricingErrors.Concurrency.Code,x.Update(3,new(2026,1,1),null,true,99,Now).Error.Code);}
    [Fact]public void Percentage_discount_is_bounded(){Assert.True(DiscountRule.Create(Company,"D",DiscountType.Percentage,DiscountScope.General,null,null,101,"IRR",new(2026,1,1),null,Now).IsFailure);}
    [Fact]public void Manual_discount_cannot_be_persisted_as_automatic_rule(){Assert.True(DiscountRule.Create(Company,"D",DiscountType.FixedAmount,DiscountScope.Manual,null,null,1,"IRR",new(2026,1,1),null,Now).IsFailure);}
    [Fact]public void Tax_rate_is_bounded(){Assert.True(TaxRule.Create(Company,"DEFAULT",100.01m,false,new(2026,1,1),null,Now).IsFailure);Assert.True(TaxRule.Create(Company,"DEFAULT",9,false,new(2026,1,1),null,Now).IsSuccess);}
    [Fact]public void Formula_is_deterministic_and_discount_never_makes_total_negative(){OrderPriceSnapshot x=OrderPriceSnapshot.Create(Company,Guid.NewGuid(),"IRR",100,20,5,3,2,999,10,false,false,Now);Assert.Equal(130,x.Subtotal);Assert.Equal(130,x.DiscountTotal);Assert.Equal(0,x.FinalTotal);}
    [Fact]public void Tax_exemption_snapshots_zero_tax(){OrderPriceSnapshot x=OrderPriceSnapshot.Create(Company,Guid.NewGuid(),"IRR",100,0,0,0,0,0,25,true,true,Now);Assert.Equal(0,x.TaxTotal);Assert.Equal(100,x.FinalTotal);Assert.True(x.IsFinalized);}
    [Fact]public void Breakdown_sum_matches_final_total(){OrderPriceSnapshot x=OrderPriceSnapshot.Create(Company,Guid.NewGuid(),"IRR",100,20,5,3,2,10,9,false,false,Now);Assert.Equal(x.FinalTotal,Money.Round(x.RawProductTotal+x.PrintTotal+x.AdditionalServiceTotal+x.PackagingTotal+x.ShippingTotal-x.DiscountTotal+x.TaxTotal));}
    [Fact]public void Customer_group_discount_matches_only_the_customer_group(){Guid group=Guid.NewGuid();DiscountRule x=DiscountRule.Create(Company,"GROUP",DiscountType.Percentage,DiscountScope.CustomerGroup,null,null,10,"IRR",new(2026,1,1),null,Now,group).Value;Assert.True(x.Applies(new(2026,8,31),Guid.NewGuid(),group,OrderSource.Internal));Assert.False(x.Applies(new(2026,8,31),Guid.NewGuid(),Guid.NewGuid(),OrderSource.Internal));}
    [Fact]public void Tax_status_is_optional_and_price_group_is_part_of_tariff_identity(){Guid group=Guid.NewGuid();Tariff x=Tariff.Create(Company,TariffType.RawProduct,"RAW",Guid.NewGuid(),null,null,10,"IRR",new(2026,1,1),null,Now,group).Value;Assert.Equal(group,x.PriceGroupId);TaxRule tax=TaxRule.Create(Company,"EXEMPT",9,true,new(2026,1,1),null,Now,"EXEMPT").Value;Assert.Equal("EXEMPT",tax.TaxStatusCode);}
}
#pragma warning restore CA1707
