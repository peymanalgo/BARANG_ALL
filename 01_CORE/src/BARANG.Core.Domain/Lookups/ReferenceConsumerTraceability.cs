namespace BARANG.Core.Domain.Lookups;

/// <summary>
/// The single auditable map from configurable catalog to its real business consumer.
/// A catalog may not be added to DomainReferenceCatalog without an entry here.
/// Entity relationships (company, branch, unit, person) are intentionally not catalog entries.
/// </summary>
public sealed record ReferenceConsumerDefinition(
    string CatalogCode,
    string Owner,
    string ConsumingEntity,
    string FieldOrRelationship,
    string Api,
    string Ui);

public static class ReferenceConsumerTraceability
{
    public static IReadOnlyList<ReferenceConsumerDefinition> Definitions { get; } =
    [
        .. DefinitionsFor("suppliers", "Supplier", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["supplier.category"] = ("Supplier", "CategoryCode", "supplier core", "Supplier > Core"),
            ["supplier.grade"] = ("Supplier", "GradeCode", "supplier core", "Supplier > Evaluation"),
            ["freight.delivery.term"] = ("SupplierSourcing", "DeliveryTermCode", "supplier sourcing", "Supplier > Sourcing"),
            ["supplier.hold.reason"] = ("SupplierLifecycle", "HoldReasonCode", "supplier lifecycle", "Supplier > Lifecycle")
        }),
        .. DefinitionsFor("customers", "Customer", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["customer.group"] = ("Customer", "CustomerGroupCode", "customer administration", "Customer > General"),
            ["price.group"] = ("Customer", "PriceGroupCode", "customer administration", "Customer > Commercial"),
            ["legal.entity.type"] = ("LegalCustomerProfile", "LegalEntityTypeCode", "customer administration", "Customer > General"),
            ["payment.term"] = ("CustomerFinancialProfile", "PaymentTermCode", "customer administration", "Customer > Financial"),
            ["payment.condition"] = ("CustomerFinancialProfile", "PaymentConditionCode", "customer administration", "Customer > Financial"),
            ["payment.method"] = ("CustomerFinancialProfile", "PaymentMethodCode", "customer administration", "Customer > Financial"),
            ["tax.status"] = ("CustomerFinancialProfile", "TaxStatusCode", "customer administration", "Customer > Financial"),
            ["address.type"] = ("CustomerAddress", "AddressTypeCode", "customer children", "Customer > Addresses"),
            ["contact.role"] = ("CustomerContact", "ContactTypeCode", "customer children", "Customer > Contacts"),
            ["bank"] = ("CustomerBankAccount", "BankCode", "customer children", "Customer > Bank Accounts"),
            ["bank.account.type"] = ("CustomerBankAccount", "AccountTypeCode", "customer children", "Customer > Bank Accounts")
        }),
        .. DefinitionsFor("products", "Product", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["product.type"] = ("Product", "ProductTypeCode", "catalog administration", "Product > Classification"),
            ["product.size"] = ("ProductVariant", "SizeCode", "catalog administration", "Product > SKUs"),
            ["product.color"] = ("ProductVariant", "ColorCode", "catalog administration", "Product > SKUs"),
            ["unit.of.measure"] = ("Product", "UnitOfMeasureCode", "catalog administration", "Product > General"),
            ["product.brand"] = ("ProductTechnicalProfile", "BrandCode", "catalog administration", "Product > Classification"),
            ["product.material"] = ("ProductTechnicalProfile", "MaterialCode", "catalog administration", "Product > Classification"),
            ["product.gender"] = ("ProductTechnicalProfile", "GenderCode", "catalog administration", "Product > Classification"),
            ["product.fit"] = ("ProductTechnicalProfile", "FitCode", "catalog administration", "Product > Classification"),
            ["product.style"] = ("ProductTechnicalProfile", "StyleCode", "catalog administration", "Product > Classification")
        }),
        .. DefinitionsFor("artwork", "Design Bank", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["artwork.category"] = ("Artwork", "CategoryId", "artworks", "Design > Classification > Category tree"),
            ["artwork.subcategory"] = ("Artwork", "SubcategoryId", "artworks", "Design > Classification"),
            ["artwork.type"] = ("Artwork", "TypeId", "artworks", "Design > Classification"),
            ["artwork.style"] = ("Artwork", "StyleId", "artworks", "Design > Classification"),
            ["artwork.theme"] = ("Artwork", "ThemeId", "artworks", "Design > Classification"),
            ["artwork.audience"] = ("Artwork", "AudienceId", "artworks", "Design > Classification"),
            ["artwork.license"] = ("Artwork", "LicenseId", "artworks", "Design > License"),
            ["artwork.usage"] = ("Artwork", "UsageId", "artworks", "Design > Usage")
        }),
        .. DefinitionsFor("equipment", "Equipment", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["equipment.type"] = ("Equipment", "EquipmentTypeCode", "equipment", "Equipment > General"),
            ["equipment.status"] = ("Equipment", "LifecycleStatusCode", "equipment", "Equipment > Lifecycle")
        }),
        .. DefinitionsFor("orders", "Orders", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["order.type"] = ("Order", "OrderTypeCode", "orders", "Orders > General")
        }),
        .. DefinitionsFor("inventory", "Inventory", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["inventory.adjustment.reason"] = ("InventoryAdjustment", "ReasonCode", "inventory", "Inventory > Adjustments")
        }),
        .. DefinitionsFor("production", "Production", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["print.method"] = ("PrintJob", "PrintMethodCode", "production", "Production > Print"),
            ["print.direction"] = ("PrintJob", "PrintDirectionCode", "production", "Production > Print"),
            ["print.size"] = ("PrintJob", "PrintSizeCode", "production", "Production > Print")
        }),
        .. DefinitionsFor("quality", "Quality", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["defect.category"] = ("Defect", "CategoryCode", "quality", "Quality > Defects")
        }),
        .. DefinitionsFor("logistics", "Logistics", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["shipping.carrier"] = ("Shipment", "CarrierCode", "logistics", "Logistics > Shipping")
        }),
        .. DefinitionsFor("shared", "Shared Basic Data", new Dictionary<string, (string Entity, string Field, string Api, string Ui)>
        {
            ["packaging.type"] = ("Shipment", "PackagingTypeCode", "logistics", "Logistics > Packaging"),
            ["shipping.method"] = ("Shipment", "ShippingMethodCode", "logistics", "Logistics > Shipping")
        }),
        new("artwork.tag", "Design Bank", "ArtworkTagAssignment", "TagId", "artwork tags", "Design > Tags"),
        new("defect.type", "Quality", "Defect", "TypeId", "defect types", "Quality > Defects")
    ];

    private static IEnumerable<ReferenceConsumerDefinition> DefinitionsFor(
        string owner, string ownerCaption,
        IReadOnlyDictionary<string, (string Entity, string Field, string Api, string Ui)> definitions) =>
        definitions.Select(x => new ReferenceConsumerDefinition(x.Key, ownerCaption, x.Value.Entity, x.Value.Field, x.Value.Api, x.Value.Ui));
}
