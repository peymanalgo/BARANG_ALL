using BARANG.Core.Domain.Customers;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class CustomerFoundationTests
{
    private static readonly DateTimeOffset Now=new(2026,8,26,12,0,0,TimeSpan.Zero);
    [Fact]public void PersonCreationNormalizesCodeContactDataAndDerivesName(){Customer x=Customer.Create(Guid.NewGuid(),Guid.NewGuid()," c-001 ",CustomerType.Person,null," Ada "," Lovelace ",null,null,null,null,"+44 (7700) 900-123",null," ADA@EXAMPLE.COM ",null,Now).Value;Assert.Equal("C-001",x.CustomerCode);Assert.Equal("Ada Lovelace",x.DisplayName);Assert.Equal("+447700900123",x.PrimaryMobile);Assert.Equal("ada@example.com",x.PrimaryEmail);}
    [Fact]public void OrganizationRequiresLegalName(){Assert.True(Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"ORG-1",CustomerType.Organization,null,null,null,null,null,null,null,null,null,null,null,Now).IsFailure);Assert.True(Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"ORG-1",CustomerType.Organization,null,null,null,"BARANG Ltd",null,null,null,null,null,null,null,Now).IsSuccess);}
    [Fact]public void PersonRequiresPersonalName()=>Assert.True(Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"P-1",CustomerType.Person,null,null,null,null,null,null,null,null,null,null,null,Now).IsFailure);
    [Theory][InlineData("bad email")][InlineData("a@")]
    public void InvalidEmailIsRejected(string email)=>Assert.Equal(CustomerErrors.InvalidEmail,Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"P-1",CustomerType.Person,null,"Name",null,null,null,null,null,null,null,email,null,Now).Error);
    [Fact]public void CustomerLifecycleRetainsStableCode(){Customer x=Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"P-1",CustomerType.Person,null,"Name",null,null,null,null,null,null,null,null,null,Now).Value;x.SetActive(false,Now.AddMinutes(1));Assert.False(x.IsActive);Assert.Equal("P-1",x.CustomerCode);Assert.Equal(2,x.Version);}
    [Fact]public void ContactRequiresMeaningfulDataAndNormalizesType(){Assert.True(CustomerContact.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()," purchasing ",null,"+1 202 555 0100",null,null,null,true,1,Now).IsSuccess);Assert.True(CustomerContact.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"general",null,null,null,null,null,false,1,Now).IsFailure);}
    [Fact]public void DeactivatingPrimaryContactClearsPrimary(){CustomerContact x=CustomerContact.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"general","Owner",null,null,null,null,true,1,Now).Value;x.SetActive(false,Now);Assert.False(x.IsActive);Assert.False(x.IsPrimary);}
    [Fact]public void AddressValidatesCountryAndRequiredLine(){Assert.True(CustomerAddress.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"shipping","Main","IR",null,"Tehran",null,"Street",null,null,null,null,true,1,Now).IsSuccess);Assert.True(CustomerAddress.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"shipping","Main","IRAN",null,null,null,"Street",null,null,null,null,false,1,Now).IsFailure);}
    [Fact]public void IdentifierAndNotesLimitsAreEnforced()=>Assert.True(Customer.Create(Guid.NewGuid(),Guid.NewGuid(),"P-1",CustomerType.Person,null,"Name",null,null,new string('x',65),null,null,null,null,null,null,Now).IsFailure);
}
