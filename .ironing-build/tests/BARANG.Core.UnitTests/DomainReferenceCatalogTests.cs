using BARANG.Core.Domain.Customers;
using BARANG.Core.Domain.Lookups;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class DomainReferenceCatalogTests
{
    private static readonly string[] SupplierOnlyDomains = ["suppliers"];
    private static readonly string[] DeferredSupplierTypes = ["supplier.evaluation","supplier.status","supplier.balance","supplier.price.list.type","bank.account.ownership"];
    private static readonly string[] SharedOnlyDomain = ["shared"];
    [Fact] public void CatalogContainsIntegratedDomainsAndUniqueCanonicalTypes()
    {
        Assert.Equal(14,DomainReferenceCatalog.Domains.Count);
        Assert.Equal(DomainReferenceCatalog.Types.Count,DomainReferenceCatalog.Types.Select(x=>x.Code).Distinct().Count());
        Assert.All(DomainReferenceCatalog.Types,type=>Assert.All(type.Domains,domain=>Assert.Contains(DomainReferenceCatalog.Domains,x=>x.Code==domain)));
    }
    [Fact] public void SupplierWorkspaceReusesOnlyApprovedCanonicalCatalogsWithoutChangingScope()
    {
        string[] expected = ["price.group","legal.entity.type","contact.role","address.type","payment.term","payment.condition","payment.method","tax.status","bank","bank.account.type","shipping.method","packaging.type","unit.of.measure"];
        var shared = DomainReferenceCatalog.Types.Where(x => x.Domains.Contains("shared") && expected.Contains(x.Code)).ToArray();
        string[] added = ["supplier.category","supplier.grade","freight.delivery.term","supplier.hold.reason"];
        var supplier = DomainReferenceCatalog.Types.Where(x => x.Domains.Contains("suppliers")).ToArray();
        Assert.Equal(added.Order(), supplier.Select(x=>x.Code).Order());
        Assert.All(added,code=>{var type=DomainReferenceCatalog.Find(code)!;Assert.Equal(LookupScope.Company,type.Scope);Assert.Equal(SupplierOnlyDomains,type.Domains);});
        Assert.Equal(expected.Order(), shared.Select(x => x.Code).Order());
        Assert.Equal(43, DomainReferenceCatalog.Types.Count);
        Assert.All(shared, type => {
            Assert.Same(DomainReferenceCatalog.Find(type.Code), type);
            Assert.Equal(SharedOnlyDomain, type.Domains);
            Assert.Equal(type.Code is "bank" or "payment.method" ? LookupScope.Global : LookupScope.Company, type.Scope);
        });
        foreach (string deferred in DeferredSupplierTypes)
            Assert.Null(DomainReferenceCatalog.Find(deferred));
    }
    [Fact] public void PricingDomainHasNoUnapprovedConfigurableCatalogs()
    {
        Assert.DoesNotContain(DomainReferenceCatalog.Types, x => x.Domains.Contains("pricing"));
        Assert.DoesNotContain(DomainReferenceCatalog.Adapters, x => x.Domains.Contains("pricing"));
        Assert.Equal("Pricing Reference Data", DomainReferenceCatalog.Domains.Single(x => x.Code == "pricing").English);
    }
    [Theory][InlineData("fa")][InlineData("ar")][InlineData("en")]
    public void EveryDomainAndCatalogHasLocalizedCaption(string culture)
    {
        Assert.All(DomainReferenceCatalog.Domains,x=>Assert.False(string.IsNullOrWhiteSpace(x.Caption(culture))));
        Assert.All(DomainReferenceCatalog.Types,x=>Assert.False(string.IsNullOrWhiteSpace(x.Caption(culture))));
    }
    [Fact] public void TechnicalEnumsAndRelationshipsAreNotEditableCatalogs()
    {
        foreach(string code in new[]{"customer.type","customer.status","customer.financial.status","order.status","workflow.priority","company","person","province","city"})
            Assert.Null(DomainReferenceCatalog.Find(code));
        Assert.Equal(LookupScope.Global,DomainReferenceCatalog.Find("bank")!.Scope);
        Assert.Equal(LookupScope.Global,DomainReferenceCatalog.Find("payment.method")!.Scope);
        Assert.Equal(LookupScope.Company,DomainReferenceCatalog.Find("customer.group")!.Scope);
    }
    [Fact] public void EveryConfigurableCatalogHasAnAuditableBusinessConsumer()
    {
        var catalogCodes = DomainReferenceCatalog.Types.Select(x => x.Code)
            .Concat(DomainReferenceCatalog.Adapters.Select(x => x.Code)).Order().ToArray();
        var consumerCodes = ReferenceConsumerTraceability.Definitions.Select(x => x.CatalogCode).Order().ToArray();
        Assert.Equal(catalogCodes, consumerCodes);
        Assert.All(ReferenceConsumerTraceability.Definitions, x =>
        {
            Assert.False(string.IsNullOrWhiteSpace(x.ConsumingEntity));
            Assert.False(string.IsNullOrWhiteSpace(x.FieldOrRelationship));
            Assert.False(string.IsNullOrWhiteSpace(x.Api));
            Assert.False(string.IsNullOrWhiteSpace(x.Ui));
        });
    }
    [Theory][InlineData("s")][InlineData("42")][InlineData("xxl")]
    public void ReferenceValueCodesSupportExistingSizeAndNumericBusinessCodes(string code)
    {
        var type=LookupType.Create(Guid.NewGuid(),Guid.NewGuid(),"product.size","Size",LookupScope.Company,false,true,DateTimeOffset.UtcNow).Value;
        Assert.Equal(code,LookupValue.Create(Guid.NewGuid(),type,code,0,false,DateTimeOffset.UtcNow).Value.Code);
    }
    [Fact] public void AddressSupplementaryFieldsRejectInvalidEmailWithoutMutation()
    {
        var address=CustomerAddress.Create(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),"shipping","Main","IR",null,null,null,"Road",null,null,null,null,false,0,DateTimeOffset.UtcNow).Value;
        Assert.True(address.AssignDetails(null,"delivery@example.com","Instructions").IsSuccess);
        Assert.True(address.AssignDetails(null,"not-an-email","Changed").IsFailure);
        Assert.Equal("delivery@example.com",address.Email);Assert.Equal("Instructions",address.Notes);
    }
}
