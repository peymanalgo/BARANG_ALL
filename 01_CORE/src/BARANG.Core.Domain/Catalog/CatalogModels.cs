using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Customers;

namespace BARANG.Core.Domain.Catalog;

public static class CatalogErrors
{
    public static Error InvalidData => Error.Validation("catalog.invalid_data"); public static Error DuplicateCode => Error.Conflict("catalog.duplicate_code"); public static Error DuplicateRelationship => Error.Conflict("catalog.duplicate_relationship"); public static Error NotFound => Error.NotFound("catalog.not_found"); public static Error CrossCompanyReference => Error.BusinessRule("catalog.cross_company_reference"); public static Error Concurrency => Error.Concurrency("catalog.concurrency_conflict");
}
public static class CatalogText
{
    public static string? Code(string? value, int max = 100) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length is >= 1 && v.Length <= max && v.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '/') ? v : null; }
    public static string? Name(string? value, int max = 200, bool optional = false) { string v = value?.Trim() ?? ""; return v.Length == 0 && optional ? null : v.Length is >= 1 && v.Length <= max ? v : null; }
    public static string? Barcode(string? value) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length == 0 ? null : v.Length <= 100 && v.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.') ? v : null; }
}
public abstract class CatalogEntity
{
    public Guid Id { get; protected set; }
    public Guid CompanyId { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset UpdatedAtUtc { get; protected set; }
    public long Version { get; protected set; }
    protected void Initialize(Guid id, Guid company, DateTimeOffset now) { Id = id; CompanyId = company; IsActive = true; CreatedAtUtc = now; UpdatedAtUtc = now; Version = 1; }
    public void SetActive(bool active, DateTimeOffset now) { if (IsActive != active) { IsActive = active; Touch(now); } }
    protected void Touch(DateTimeOffset now) { UpdatedAtUtc = now; Version++; }
}
public sealed class Product : CatalogEntity
{
    private Product() { }
    public string ProductCode { get; private set; } = ""; public string Name { get; private set; } = ""; public string? Description { get; private set; }
    public string ProductTypeCode { get; private set; } = ""; public string UnitOfMeasureCode { get; private set; } = "";
    public static Result<Product> Create(Guid id, Guid company, string code, string name, string? description, string type, string uom, DateTimeOffset now) { string? c = CatalogText.Code(code, 50), n = CatalogText.Name(name), d = CatalogText.Name(description, 2000, true), t = CatalogText.Code(type, 50), u = CatalogText.Code(uom, 30); if (id == Guid.Empty || company == Guid.Empty || c is null || n is null || t is null || u is null || (!string.IsNullOrWhiteSpace(description) && d is null)) return Result.Failure<Product>(CatalogErrors.InvalidData); Product x = new() { ProductCode = c, Name = n, Description = d, ProductTypeCode = t, UnitOfMeasureCode = u }; x.Initialize(id, company, now); return Result.Success(x); }
    public Result Update(string name, string? description, string type, string uom, DateTimeOffset now) { string? n = CatalogText.Name(name), d = CatalogText.Name(description, 2000, true), t = CatalogText.Code(type, 50), unit = CatalogText.Code(uom, 30); if (n is null || t is null || unit is null || (!string.IsNullOrWhiteSpace(description) && d is null)) return Result.Failure(CatalogErrors.InvalidData); Name = n; Description = d; ProductTypeCode = t; UnitOfMeasureCode = unit; Touch(now); return Result.Success(); }
}
public sealed class ProductVariant : CatalogEntity
{
    private ProductVariant() { }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = ""; public string? DisplayName { get; private set; }
    public string? Barcode { get; private set; }
    public string? SizeCode { get; private set; }
    public string? ColorCode { get; private set; }
    public static Result<ProductVariant> Create(Guid id, Product product, string sku, string? name, string? barcode, string? size, string? color, DateTimeOffset now) { string? s = CatalogText.Code(sku, 80), n = CatalogText.Name(name, 200, true), b = CatalogText.Barcode(barcode), z = string.IsNullOrWhiteSpace(size) ? null : CatalogText.Code(size, 50), c = string.IsNullOrWhiteSpace(color) ? null : CatalogText.Code(color, 50); if (id == Guid.Empty || s is null || (!string.IsNullOrWhiteSpace(name) && n is null) || (!string.IsNullOrWhiteSpace(barcode) && b is null) || (!string.IsNullOrWhiteSpace(size) && z is null) || (!string.IsNullOrWhiteSpace(color) && c is null)) return Result.Failure<ProductVariant>(CatalogErrors.InvalidData); ProductVariant x = new() { ProductId = product.Id, Sku = s, DisplayName = n, Barcode = b, SizeCode = z, ColorCode = c }; x.Initialize(id, product.CompanyId, now); return Result.Success(x); }
    public Result Update(string? name, string? barcode, string? size, string? color, DateTimeOffset now) { string? n = CatalogText.Name(name, 200, true), b = CatalogText.Barcode(barcode), z = string.IsNullOrWhiteSpace(size) ? null : CatalogText.Code(size, 50), c = string.IsNullOrWhiteSpace(color) ? null : CatalogText.Code(color, 50); if ((!string.IsNullOrWhiteSpace(name) && n is null) || (!string.IsNullOrWhiteSpace(barcode) && b is null) || (!string.IsNullOrWhiteSpace(size) && z is null) || (!string.IsNullOrWhiteSpace(color) && c is null)) return Result.Failure(CatalogErrors.InvalidData); DisplayName = n; Barcode = b; SizeCode = z; ColorCode = c; Touch(now); return Result.Success(); }
}

/// <summary>Authoritative customer-facing sales price for a raw Product/SKU.</summary>
public sealed class ProductSalesPrice : CatalogEntity
{
    private ProductSalesPrice() { }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public Guid? MigratedFromTariffId { get; private set; }

    public static Result<ProductSalesPrice> Create(Guid id, Guid company, Guid product, Guid? variant, decimal amount, string currency, DateOnly start, DateOnly? end, DateTimeOffset now, Guid? migratedFromTariffId = null)
    {
        string code = (currency ?? "").Trim().ToUpperInvariant();
        if (id == Guid.Empty || company == Guid.Empty || product == Guid.Empty || amount < 0 || code.Length != 3 || !code.All(char.IsAsciiLetter) || end < start)
            return Result.Failure<ProductSalesPrice>(CatalogErrors.InvalidData);
        ProductSalesPrice x = new() { ProductId = product, ProductVariantId = variant, Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero), CurrencyCode = code, StartDate = start, EndDate = end, MigratedFromTariffId = migratedFromTariffId };
        x.Initialize(id, company, now);
        return Result.Success(x);
    }

    public Result Update(decimal amount, string currency, DateOnly start, DateOnly? end, long expectedVersion, DateTimeOffset now)
    {
        string code = (currency ?? "").Trim().ToUpperInvariant();
        if (expectedVersion != Version || amount < 0 || code.Length != 3 || !code.All(char.IsAsciiLetter) || end < start)
            return expectedVersion != Version ? Result.Failure(CatalogErrors.Concurrency) : Result.Failure(CatalogErrors.InvalidData);
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero); CurrencyCode = code; StartDate = start; EndDate = end; Touch(now); return Result.Success();
    }

    public bool Applies(DateOnly pricingDate) => IsActive && StartDate <= pricingDate && (EndDate is null || EndDate >= pricingDate);
}
public sealed partial class Supplier : CatalogEntity
{
    private Supplier() { }
    public string SupplierCode { get; private set; } = ""; public string DisplayName { get; private set; } = ""; public string? LegalName { get; private set; }
    public string? RegistrationNo { get; private set; }
    public string? TaxId { get; private set; }
    public string? PrimaryEmail { get; private set; }
    public string? PrimaryPhone { get; private set; }
    public string? PrimaryMobile { get; private set; }
    public string? Website { get; private set; }
    public string? Notes { get; private set; }
    public static Result<Supplier> Create(Guid id, Guid company, string code, string name, string? legal, string? registration, string? tax, string? email, string? phone, string? mobile, string? website, string? notes, DateTimeOffset now) { string? c = CatalogText.Code(code, 50); if (id == Guid.Empty || company == Guid.Empty || c is null) return Result.Failure<Supplier>(CatalogErrors.InvalidData); Supplier x = new() { SupplierCode = c, EntityType = string.IsNullOrWhiteSpace(legal) ? "Individual" : "LegalEntity" }; x.Initialize(id, company, now); Result r = x.Update(name, legal, registration, tax, email, phone, mobile, website, notes, now, false); return r.IsFailure ? Result.Failure<Supplier>(r.Error) : Result.Success(x); }
    public Result Update(string name, string? legal, string? registration, string? tax, string? email, string? phone, string? mobile, string? website, string? notes, DateTimeOffset now) => Update(name, legal, registration, tax, email, phone, mobile, website, notes, now, true); private Result Update(string name, string? legal, string? registration, string? tax, string? email, string? phone, string? mobile, string? website, string? notes, DateTimeOffset now, bool touch) { string? n = CatalogText.Name(name), l = CatalogText.Name(legal, 200, true), r = CustomerText.Identifier(registration), t = CustomerText.Identifier(tax), e = CustomerText.Email(email), p = CustomerText.Phone(phone), m = CustomerText.Phone(mobile), w = CatalogText.Name(website, 500, true), no = CatalogText.Name(notes, 2000, true); if (n is null || e == "" || p == "" || m == "" || (!string.IsNullOrWhiteSpace(legal) && l is null) || (!string.IsNullOrWhiteSpace(registration) && r is null) || (!string.IsNullOrWhiteSpace(tax) && t is null) || (!string.IsNullOrWhiteSpace(website) && w is null) || (!string.IsNullOrWhiteSpace(notes) && no is null)) return Result.Failure(CatalogErrors.InvalidData); DisplayName = n; LegalName = l; RegistrationNo = r; TaxId = t; PrimaryEmail = e; PrimaryPhone = p; PrimaryMobile = m; Website = w; Notes = no; if (touch) Touch(now); return Result.Success(); }
}
public sealed partial class SupplierProduct : CatalogEntity
{
    private SupplierProduct() { }
    public Guid SupplierId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public string? SupplierItemCode { get; private set; }
    public bool IsPreferred { get; private set; }
    public decimal? MinimumOrderQuantity { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public static Result<SupplierProduct> Create(Guid id, Guid company, Guid supplier, Guid product, Guid? variant, string? item, bool preferred, decimal? moq, int? lead, DateTimeOffset now) { if (id == Guid.Empty || company == Guid.Empty || supplier == Guid.Empty || product == Guid.Empty) return Result.Failure<SupplierProduct>(CatalogErrors.InvalidData); SupplierProduct x = new() { SupplierId = supplier, ProductId = product, ProductVariantId = variant }; x.Initialize(id, company, now); Result r = x.Update(item, preferred, moq, lead, now, false); return r.IsFailure ? Result.Failure<SupplierProduct>(r.Error) : Result.Success(x); }
    public Result Update(string? item, bool preferred, decimal? moq, int? lead, DateTimeOffset now) => Update(item, preferred, moq, lead, now, true); private Result Update(string? item, bool preferred, decimal? moq, int? lead, DateTimeOffset now, bool touch) { string? i = string.IsNullOrWhiteSpace(item) ? null : CatalogText.Code(item, 80); if ((!string.IsNullOrWhiteSpace(item) && i is null) || moq < 0 || lead < 0) return Result.Failure(CatalogErrors.InvalidData); SupplierItemCode = i; IsPreferred = preferred; MinimumOrderQuantity = moq; LeadTimeDays = lead; if (touch) Touch(now); return Result.Success(); }
}
public sealed class Equipment : CatalogEntity
{
    private Equipment() { }
    public string EquipmentCode { get; private set; } = ""; public string Name { get; private set; } = ""; public string EquipmentTypeCode { get; private set; } = ""; public string OperationalStatusCode { get; private set; } = ""; public Guid? BranchId { get; private set; }
    public Guid? OrganizationalUnitId { get; private set; }
    public string? AssetNumber { get; private set; } public string? AssetTag { get; private set; } public string? EquipmentCategoryCode { get; private set; } public string? EquipmentSubtypeCode { get; private set; } public string? BrandCode { get; private set; } public string? ManufacturerCode { get; private set; } public string? ConditionCode { get; private set; } public string? CriticalityCode { get; private set; } public string? OwnershipTypeCode { get; private set; } public string? AcquisitionMethodCode { get; private set; } public string? LifecycleStatusCode { get; private set; }
    public string? Description { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }
    public int? CapacityPerHour { get; private set; }
    public Result SetCapacity(int? capacityPerHour, DateTimeOffset now) { if (capacityPerHour is < 0 or > 100000) return Result.Failure(CatalogErrors.InvalidData); if (CapacityPerHour != capacityPerHour) { CapacityPerHour = capacityPerHour; Touch(now); } return Result.Success(); }
    public string? PhysicalLocation { get; private set; } public Guid? WarehouseId { get; private set; } public Guid? ResponsiblePersonId { get; private set; } public Guid? AssignedPersonId { get; private set; } public DateOnly? PurchaseDate { get; private set; } public decimal? PurchasePrice { get; private set; } public string? CurrencyCode { get; private set; } public Guid? SupplierId { get; private set; } public string? PurchaseReference { get; private set; } public DateOnly? CommissioningDate { get; private set; } public DateOnly? WarrantyStart { get; private set; } public DateOnly? WarrantyEnd { get; private set; } public string? WarrantyProvider { get; private set; } public string? WarrantyNotes { get; private set; } public int? ExpectedUsefulLifeMonths { get; private set; } public DateOnly? RetirementDate { get; private set; } public string? RetirementReasonCode { get; private set; } public string? Notes { get; private set; }
    public static Result<Equipment> Create(Guid id, Guid company, string code, string name, string type, string status, Guid? branch, Guid? unit, string? description, string? manufacturer, string? model, string? serial, DateTimeOffset now) { string? c = CatalogText.Code(code, 50); if (id == Guid.Empty || company == Guid.Empty || c is null) return Result.Failure<Equipment>(CatalogErrors.InvalidData); Equipment x = new() { EquipmentCode = c }; x.Initialize(id, company, now); Result r = x.Update(name, type, status, branch, unit, description, manufacturer, model, serial, now, false); return r.IsFailure ? Result.Failure<Equipment>(r.Error) : Result.Success(x); }
    public Result Update(string name, string type, string status, Guid? branch, Guid? unit, string? description, string? manufacturer, string? model, string? serial, DateTimeOffset now) => Update(name, type, status, branch, unit, description, manufacturer, model, serial, now, true); private Result Update(string name, string type, string status, Guid? branch, Guid? unit, string? description, string? manufacturer, string? model, string? serial, DateTimeOffset now, bool touch) { string? n = CatalogText.Name(name), t = CatalogText.Code(type, 50), s = CatalogText.Code(status, 50), d = CatalogText.Name(description, 2000, true), man = CatalogText.Name(manufacturer, 150, true), mo = CatalogText.Name(model, 150, true), ser = CatalogText.Name(serial, 150, true); if (n is null || t is null || s is null || (!string.IsNullOrWhiteSpace(description) && d is null) || (!string.IsNullOrWhiteSpace(manufacturer) && man is null) || (!string.IsNullOrWhiteSpace(model) && mo is null) || (!string.IsNullOrWhiteSpace(serial) && ser is null)) return Result.Failure(CatalogErrors.InvalidData); Name = n; EquipmentTypeCode = t; OperationalStatusCode = s; BranchId = branch; OrganizationalUnitId = unit; Description = d; Manufacturer = man; Model = mo; SerialNumber = ser; if (touch) Touch(now); return Result.Success(); }
    public Result UpdateDetails(string name, string type, string status, string lifecycle, Guid? branch, Guid? unit, string? assetNumber, string? assetTag, string? category, string? subtype, string? brand, string? manufacturerCode, string? condition, string? criticality, string? ownership, string? acquisition, string? description, string? manufacturer, string? model, string? serial, string? physicalLocation, Guid? warehouse, Guid? responsiblePerson, Guid? assignedPerson, DateOnly? purchaseDate, decimal? purchasePrice, string? currency, Guid? supplier, string? purchaseReference, DateOnly? commissioningDate, DateOnly? warrantyStart, DateOnly? warrantyEnd, string? warrantyProvider, string? warrantyNotes, int? usefulLifeMonths, DateOnly? retirementDate, string? retirementReason, string? notes, DateTimeOffset now)
    {
        Result basic = Update(name, type, status, branch, unit, description, manufacturer, model, serial, now, false); if (basic.IsFailure) return basic;
        string? life = CatalogText.Code(lifecycle, 50); if (life is null || purchasePrice < 0 || usefulLifeMonths < 0 || (warrantyStart is not null && warrantyEnd is not null && warrantyEnd < warrantyStart) || (retirementDate is not null && commissioningDate is not null && retirementDate < commissioningDate)) return Result.Failure(CatalogErrors.InvalidData);
        AssetNumber = Optional(assetNumber, 80); AssetTag = Optional(assetTag, 80); EquipmentCategoryCode = OptionalCode(category); EquipmentSubtypeCode = OptionalCode(subtype); BrandCode = OptionalCode(brand); ManufacturerCode = OptionalCode(manufacturerCode); ConditionCode = OptionalCode(condition); CriticalityCode = OptionalCode(criticality); OwnershipTypeCode = OptionalCode(ownership); AcquisitionMethodCode = OptionalCode(acquisition); LifecycleStatusCode = life; PhysicalLocation = Optional(physicalLocation, 250); WarehouseId = warehouse; ResponsiblePersonId = responsiblePerson; AssignedPersonId = assignedPerson; PurchaseDate = purchaseDate; PurchasePrice = purchasePrice; CurrencyCode = OptionalCode(currency); SupplierId = supplier; PurchaseReference = Optional(purchaseReference, 120); CommissioningDate = commissioningDate; WarrantyStart = warrantyStart; WarrantyEnd = warrantyEnd; WarrantyProvider = Optional(warrantyProvider, 200); WarrantyNotes = Optional(warrantyNotes, 1000); ExpectedUsefulLifeMonths = usefulLifeMonths; RetirementDate = retirementDate; RetirementReasonCode = OptionalCode(retirementReason); Notes = Optional(notes, 2000); Touch(now); return Result.Success();
    }
    private static string? Optional(string? value, int max) { string v = value?.Trim() ?? ""; return v.Length == 0 ? null : v.Length <= max ? v : null; }
    public Result SetLifecycle(string status,DateOnly? retirementDate,string? reason,DateTimeOffset now){var next=OptionalCode(status);if(next is null||next=="RETIRED"&&retirementDate is null)return Result.Failure(CatalogErrors.InvalidData);LifecycleStatusCode=next;RetirementDate=retirementDate;RetirementReasonCode=OptionalCode(reason);Touch(now);return Result.Success();}
    private static string? OptionalCode(string? value) { string v = value?.Trim().ToUpperInvariant() ?? ""; return v.Length == 0 ? null : CatalogText.Code(v, 80); }
}
