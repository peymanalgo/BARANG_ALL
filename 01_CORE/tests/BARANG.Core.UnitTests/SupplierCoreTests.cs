using BARANG.Core.Domain.Catalog;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class SupplierCoreTests
{
    private static SupplierIdentityData Identity=>new(SupplierCode:"SUP-1",DisplayName:"Example supplier");
    [Fact]public void IndividualDoesNotRequireUnnecessaryPersonalDataAndCodeIsImmutable(){
        var value=Supplier.CreateCore(Guid.NewGuid(),Guid.NewGuid(),Identity,DateTimeOffset.UtcNow).Value;
        Assert.Equal("Individual",value.EntityType);Assert.Null(value.NationalId);Assert.Null(value.FirstName);Assert.Equal(1,value.Version);
        Assert.True(value.UpdateCore(Identity with{SupplierCode="OTHER"},DateTimeOffset.UtcNow).IsFailure);
        Assert.Equal("SUP-1",value.SupplierCode);Assert.Equal(1,value.Version);
    }
    [Fact]public void InvalidIdentityDoesNotMutateTheAggregate(){
        var value=Supplier.CreateCore(Guid.NewGuid(),Guid.NewGuid(),Identity,DateTimeOffset.UtcNow).Value;
        Assert.True(value.UpdateCore(Identity with{DisplayName="Changed",PrimaryEmail="invalid"},DateTimeOffset.UtcNow).IsFailure);
        Assert.Equal("Example supplier",value.DisplayName);Assert.Equal(1,value.Version);
    }
    [Fact]public void LegalFieldsAndCanonicalDatesRoundTrip(){
        DateOnly date=new(1985,6,8);
        var value=Supplier.CreateCore(Guid.NewGuid(),Guid.NewGuid(),Identity with{EntityType="LegalEntity",LegalName="Legal supplier",TradeName="Trade",EstablishmentDate=date,CooperationStartDate=date,NationalId="NATIONAL"},DateTimeOffset.UtcNow).Value;
        Assert.Equal(date,value.EstablishmentDate);Assert.Equal(date,value.CooperationStartDate);Assert.Equal("Trade",value.TradeName);Assert.Equal("NATIONAL",value.NationalId);
    }
    [Fact]public void BlockingRequiresReasonAndRemovesActiveAvailability(){
        var value=Supplier.CreateCore(Guid.NewGuid(),Guid.NewGuid(),Identity,DateTimeOffset.UtcNow).Value;
        Assert.True(value.SetCoreStatus("Blocked",null,DateTimeOffset.UtcNow).IsFailure);Assert.True(value.IsActive);
        Assert.True(value.SetCoreStatus("Blocked",Guid.NewGuid(),DateTimeOffset.UtcNow).IsSuccess);Assert.True(value.IsBlocked);Assert.False(value.IsActive);
        Assert.True(value.SetCoreStatus("Active",null,DateTimeOffset.UtcNow).IsSuccess);Assert.True(value.IsActive);Assert.Null(value.HoldReasonId);
    }
    [Theory][InlineData("address")][InlineData("contact")][InlineData("bank")]
    public void InactiveChildCannotBecomeSelectedAndDeactivationClearsSelection(string kind){
        Guid owner=Guid.NewGuid(),company=Guid.NewGuid();DateTimeOffset now=DateTimeOffset.UtcNow;
        SupplierOwnedEntity value=kind switch{
            "address"=>SupplierAddress.Create(owner,company,new(AddressName:"Main",AddressTypeId:Guid.NewGuid(),FullAddress:"Road",IsDefault:true),now).Value,
            "contact"=>SupplierContact.Create(owner,company,new(FirstName:"Contact",ContactRoleId:Guid.NewGuid(),IsPrimary:true),now).Value,
            _=>SupplierBankAccount.Create(owner,company,new(BankId:Guid.NewGuid(),AccountHolderNames:"Holder",IsDefault:true),now).Value};
        Assert.Equal(owner,value.SupplierId);Assert.Equal(company,value.CompanyId);Assert.True(value.IsDefault);
        value.ChangeActivation(false,now);Assert.False(value.IsActive);Assert.False(value.IsDefault);
        Assert.Equal("supplier.inactive_default",value.SelectDefault(true,now).Error.Code);
        value.ChangeActivation(true,now);Assert.False(value.IsDefault);
    }
    [Theory][InlineData(-1)][InlineData(101)]
    public void FinancialPercentBoundsAreServerDomainRules(decimal discount){
        Assert.True(SupplierCoreRules.Financial(new(BaseDiscountPercent:discount)).IsFailure);
        Assert.True(SupplierCoreRules.Financial(new(PurchaseCreditLimit:-1)).IsFailure);
        Assert.True(SupplierCoreRules.Financial(new(DefaultLeadTimeDays:-1)).IsFailure);
    }
    [Fact]public void BankAccountAllowsJointOwnershipAndRejectsMalformedFormats(){
        var data=new SupplierBankData(BankId:Guid.NewGuid(),AccountHolderNames:"First; Second",OwnershipType:"Joint",CardNumber:"6037991234567890");
        Assert.True(SupplierCoreRules.Bank(data).IsSuccess);
        Assert.True(SupplierCoreRules.Bank(data with{Iban="not-an-iban"}).IsFailure);
        Assert.True(SupplierCoreRules.Bank(data with{CardNumber="1234"}).IsFailure);
        Assert.DoesNotContain(typeof(SupplierBankAccount).GetProperties(),p=>new[]{"Pin","Cvv","Password"}.Contains(p.Name));
    }
}
