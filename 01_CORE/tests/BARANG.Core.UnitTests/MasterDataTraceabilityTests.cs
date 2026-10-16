using BARANG.Core.Domain.Lookups;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class MasterDataTraceabilityTests
{
    private static readonly string[] EquipmentDomains = ["equipment", "inventory"];

    [Fact]
    public void EveryCatalogTypeHasATraceableConsumerOrExplicitAdapter()
    {
        string[] catalogCodes = DomainReferenceCatalog.Types
            .Concat(DomainReferenceCatalog.Adapters)
            .Select(x => x.Code)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        string[] consumerCodes = ReferenceConsumerTraceability.Definitions
            .Select(x => x.CatalogCode)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(catalogCodes, consumerCodes);
    }

    [Fact]
    public void EquipmentStatusCatalogPreservesLegacyInventoryRouteAndOwnsBothStatusConsumers()
    {
        DomainReferenceDefinition status = Assert.Single(DomainReferenceCatalog.Types, x => x.Code == "equipment.status");
        Assert.Equal(EquipmentDomains, status.Domains);

        ReferenceConsumerDefinition consumer = Assert.Single(ReferenceConsumerTraceability.Definitions, x => x.CatalogCode == "equipment.status");
        Assert.Contains("OperationalStatusCode", consumer.FieldOrRelationship, StringComparison.Ordinal);
        Assert.Contains("LifecycleStatusCode", consumer.FieldOrRelationship, StringComparison.Ordinal);
    }
}
