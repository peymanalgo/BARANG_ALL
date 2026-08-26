using BARANG.Core.Domain.Lookups;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class LookupFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026,8,26,10,0,0,TimeSpan.Zero);

    [Fact] public void TypeNormalizesTechnicalCodeAndEnforcesScope()
    {
        Guid company=Guid.NewGuid(); var result=LookupType.Create(Guid.NewGuid(),company," Payment.Method ","Payment method",LookupScope.Company,false,false,Now);
        Assert.True(result.IsSuccess); Assert.Equal("payment.method",result.Value.Code);
        Assert.True(LookupType.Create(Guid.NewGuid(),null,"x.y","X",LookupScope.Company,false,false,Now).IsFailure);
    }
    [Fact] public void SystemTypeIsProtected()
    {
        var type=LookupType.Create(Guid.NewGuid(),null,"system.test","System",LookupScope.Global,true,false,Now).Value;
        Assert.Equal(LookupErrors.SystemRecordProtected,type.Update("Changed",false,Now).Error);
        Assert.Equal(LookupErrors.SystemRecordProtected,type.SetActive(false,Now).Error);
    }
    [Fact] public void ValueCodesAreInvariantAndTranslationsAreNormalized()
    {
        var type=LookupType.Create(Guid.NewGuid(),Guid.NewGuid(),"test.kind","Kind",LookupScope.Company,false,false,Now).Value;
        var value=LookupValue.Create(Guid.NewGuid(),type," FIRST_VALUE ",10,false,Now).Value;
        Assert.Equal("first_value",value.Code); Assert.True(value.SetTranslation("FA-ir","اول",null,Now).IsSuccess); Assert.Equal("fa",Assert.Single(value.Translations).Culture);
    }
    [Theory] [InlineData("fa")] [InlineData("en-US")] [InlineData("AR-ir")]
    public void SupportedCulturesNormalize(string culture)=>Assert.NotNull(LookupText.NormalizeCulture(culture));
    [Fact] public void UnsupportedCultureAndBlankTranslationAreRejected()
    {
        var type=LookupType.Create(Guid.NewGuid(),Guid.NewGuid(),"test.kind","Kind",LookupScope.Company,false,false,Now).Value;
        var value=LookupValue.Create(Guid.NewGuid(),type,"one",0,false,Now).Value;
        Assert.Equal(LookupErrors.UnsupportedCulture,value.SetTranslation("de","Eins",null,Now).Error);
        Assert.Equal(LookupErrors.InvalidName,value.SetTranslation("en"," ",null,Now).Error);
    }
    [Fact] public void InactiveValueRetainsStableIdentityAndCode()
    {
        var type=LookupType.Create(Guid.NewGuid(),Guid.NewGuid(),"test.kind","Kind",LookupScope.Company,false,false,Now).Value;
        var value=LookupValue.Create(Guid.NewGuid(),type,"one",0,false,Now).Value; Guid id=value.Id;
        Assert.True(value.SetActive(false,Now.AddMinutes(1)).IsSuccess); Assert.False(value.IsActive); Assert.Equal(id,value.Id); Assert.Equal("one",value.Code);
    }
}
