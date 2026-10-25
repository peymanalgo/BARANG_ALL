using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Orders;

namespace BARANG.Core.Domain.Pricing;

public enum TariffType { RawProduct, Print, Packaging, Shipping, Design, AdditionalService }
public enum DiscountType { FixedAmount, Percentage }
public enum DiscountScope { General, Customer, CustomerGroup, PartnerSource, Manual }
public enum PriceComponentType { RawProduct, Print, AdditionalService, Packaging, Shipping, Discount, Tax }

public static class PricingErrors
{
    public static Error InvalidData => Error.Validation("pricing.invalid_data");
    public static Error InvalidPeriod => Error.Validation("pricing.invalid_period");
    public static Error InvalidCurrency => Error.Validation("pricing.currency_mismatch");
    public static Error Overlap => Error.Conflict("pricing.tariff.overlap");
    public static Error NotFound => Error.NotFound("pricing.resource.not_found");
    public static Error MissingTariff => Error.BusinessRule("pricing.tariff.missing");
    public static Error NotDraft => Error.BusinessRule("pricing.order.not_draft");
    public static Error Concurrency => Error.Conflict("pricing.concurrency_conflict");
}

public readonly record struct Money(decimal Amount, string CurrencyCode)
{
    public static Result<Money> Create(decimal amount, string currency)
    {
        string code = (currency ?? string.Empty).Trim().ToUpperInvariant();
        return amount < 0 || code.Length != 3 || !code.All(char.IsAsciiLetter)
            ? Result.Failure<Money>(PricingErrors.InvalidData)
            : Result.Success(new Money(Round(amount), code));
    }
    public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}

public sealed class Tariff
{
    private Tariff() { }
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public TariffType TariffType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public Guid? ProductVariantId { get; private set; }
    public Guid? PriceGroupId { get; private set; }
    public string? PrintPositionCode { get; private set; }
    public string? PrintMethodCode { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = string.Empty;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public static Result<Tariff> Create(Guid companyId, TariffType type, string code, Guid? variantId, string? position, string? method, decimal amount, string currency, DateOnly from, DateOnly? to, DateTimeOffset now, Guid? priceGroupId = null)
    {
        string key = Normalize(code); string? p = Optional(position); string? m = Optional(method); Result<Money> money = Money.Create(amount, currency);
        if (companyId == Guid.Empty || key.Length is < 1 or > 80 || to < from || money.IsFailure || type == TariffType.RawProduct && variantId is null || type != TariffType.RawProduct && variantId is not null) return Result.Failure<Tariff>(PricingErrors.InvalidData);
        return Result.Success(new Tariff { Id = Guid.NewGuid(), CompanyId = companyId, TariffType = type, Code = key, ProductVariantId = variantId, PriceGroupId = priceGroupId, PrintPositionCode = p, PrintMethodCode = m, Amount = money.Value.Amount, CurrencyCode = money.Value.CurrencyCode, ValidFrom = from, ValidTo = to, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public Result Update(decimal amount, DateOnly from, DateOnly? to, bool active, long expectedVersion, DateTimeOffset now, Guid? priceGroupId = null) { Result<Money> m = Money.Create(amount, CurrencyCode); if (expectedVersion != Version) return Result.Failure(PricingErrors.Concurrency); if (m.IsFailure || to < from) return Result.Failure(PricingErrors.InvalidData); Amount = m.Value.Amount; ValidFrom = from; ValidTo = to; IsActive = active; PriceGroupId = priceGroupId; UpdatedAtUtc = now; Version++; return Result.Success(); }
    public bool Applies(DateOnly date) => IsActive && ValidFrom <= date && (ValidTo is null || ValidTo >= date);
    internal static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    internal static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : Normalize(value);
}

public sealed class DiscountRule
{
    private DiscountRule() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public string Code { get; private set; } = string.Empty; public DiscountType DiscountType { get; private set; } public DiscountScope Scope { get; private set; } public Guid? CustomerId { get; private set; } public Guid? CustomerGroupId { get; private set; } public OrderSource? Source { get; private set; } public decimal Value { get; private set; } public string CurrencyCode { get; private set; } = string.Empty; public DateOnly ValidFrom { get; private set; } public DateOnly? ValidTo { get; private set; } public bool IsActive { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<DiscountRule> Create(Guid companyId, string code, DiscountType type, DiscountScope scope, Guid? customer, OrderSource? source, decimal value, string currency, DateOnly from, DateOnly? to, DateTimeOffset now, Guid? customerGroupId = null)
    {
        string c = Tariff.Normalize(code); string cur = Tariff.Normalize(currency); bool badScope = scope == DiscountScope.Customer && customer is null || scope == DiscountScope.PartnerSource && source is null || scope == DiscountScope.Manual;
        bool badGroup = scope == DiscountScope.CustomerGroup && customerGroupId is null;
        if (companyId == Guid.Empty || c.Length is < 1 or > 80 || cur.Length != 3 || value < 0 || type == DiscountType.Percentage && value > 100 || to < from || badScope || badGroup) return Result.Failure<DiscountRule>(PricingErrors.InvalidData);
        return Result.Success(new DiscountRule { Id = Guid.NewGuid(), CompanyId = companyId, Code = c, DiscountType = type, Scope = scope, CustomerId = customer, CustomerGroupId = customerGroupId, Source = source, Value = value, CurrencyCode = cur, ValidFrom = from, ValidTo = to, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 });
    }
    public bool Applies(DateOnly date, Guid customer, Guid? customerGroup, OrderSource source) => IsActive && ValidFrom <= date && (ValidTo is null || ValidTo >= date) && (Scope == DiscountScope.General || Scope == DiscountScope.Customer && CustomerId == customer || Scope == DiscountScope.CustomerGroup && CustomerGroupId == customerGroup || Scope == DiscountScope.PartnerSource && Source == source);
}

public sealed class TaxRule
{
    private TaxRule() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public string Code { get; private set; } = string.Empty; public string? TaxStatusCode { get; private set; } public decimal RatePercent { get; private set; } public bool IsExempt { get; private set; } public DateOnly ValidFrom { get; private set; } public DateOnly? ValidTo { get; private set; } public bool IsActive { get; private set; } public DateTimeOffset CreatedAtUtc { get; private set; } public DateTimeOffset UpdatedAtUtc { get; private set; } public long Version { get; private set; }
    public static Result<TaxRule> Create(Guid company, string code, decimal rate, bool exempt, DateOnly from, DateOnly? to, DateTimeOffset now, string? taxStatusCode = null) { string c = Tariff.Normalize(code); string? status = string.IsNullOrWhiteSpace(taxStatusCode) ? null : Tariff.Normalize(taxStatusCode); if (company == Guid.Empty || c.Length is < 1 or > 80 || status?.Length > 80 || rate is < 0 or > 100 || to < from) return Result.Failure<TaxRule>(PricingErrors.InvalidData); return Result.Success(new TaxRule { Id = Guid.NewGuid(), CompanyId = company, Code = c, TaxStatusCode = status, RatePercent = rate, IsExempt = exempt, ValidFrom = from, ValidTo = to, IsActive = true, CreatedAtUtc = now, UpdatedAtUtc = now, Version = 1 }); }
    public bool Applies(DateOnly date) => IsActive && ValidFrom <= date && (ValidTo is null || ValidTo >= date);
}

public sealed class OrderPriceSnapshot
{
    private OrderPriceSnapshot() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderId { get; private set; } public string CurrencyCode { get; private set; } = string.Empty; public decimal RawProductTotal { get; private set; } public decimal PrintTotal { get; private set; } public decimal AdditionalServiceTotal { get; private set; } public decimal PackagingTotal { get; private set; } public decimal ShippingTotal { get; private set; } public decimal Subtotal { get; private set; } public decimal DiscountTotal { get; private set; } public decimal TaxableTotal { get; private set; } public decimal TaxTotal { get; private set; } public decimal FinalTotal { get; private set; } public decimal TaxRatePercent { get; private set; } public bool IsTaxExempt { get; private set; } public bool IsCurrent { get; private set; } public bool IsFinalized { get; private set; } public DateTimeOffset CalculatedAtUtc { get; private set; } public DateTimeOffset? FinalizedAtUtc { get; private set; } public int CalculationVersion { get; private set; } public long Version { get; private set; }
    public static OrderPriceSnapshot Create(Guid company, Guid order, string currency, decimal raw, decimal print, decimal services, decimal packaging, decimal shipping, decimal discount, decimal taxRate, bool exempt, bool finalized, DateTimeOffset now) { decimal subtotal = Money.Round(raw + print + services + packaging + shipping); decimal appliedDiscount = Math.Min(Money.Round(discount), subtotal); decimal taxable = Money.Round(subtotal - appliedDiscount); decimal tax = exempt ? 0 : Money.Round(taxable * taxRate / 100m); return new() { Id = Guid.NewGuid(), CompanyId = company, OrderId = order, CurrencyCode = currency, RawProductTotal = Money.Round(raw), PrintTotal = Money.Round(print), AdditionalServiceTotal = Money.Round(services), PackagingTotal = Money.Round(packaging), ShippingTotal = Money.Round(shipping), Subtotal = subtotal, DiscountTotal = appliedDiscount, TaxableTotal = taxable, TaxTotal = tax, FinalTotal = Money.Round(taxable + tax), TaxRatePercent = taxRate, IsTaxExempt = exempt, IsCurrent = true, IsFinalized = finalized, CalculatedAtUtc = now, FinalizedAtUtc = finalized ? now : null, CalculationVersion = 1, Version = 1 }; }
    public Result Supersede() { if (IsFinalized) return Result.Failure(PricingErrors.NotDraft); IsCurrent = false; Version++; return Result.Success(); }
    public Result SupersedeForApprovedRevision() { IsCurrent = false; Version++; return Result.Success(); }
    public void FinalizeSnapshot(DateTimeOffset now) { IsFinalized = true; FinalizedAtUtc = now; Version++; }
}

public sealed class OrderItemPriceSnapshot
{
    private OrderItemPriceSnapshot() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderPriceSnapshotId { get; private set; } public Guid OrderItemId { get; private set; } public decimal RawProductTotal { get; private set; } public decimal PrintTotal { get; private set; } public decimal AdditionalServiceTotal { get; private set; } public decimal Subtotal { get; private set; } public decimal DiscountAllocated { get; private set; } public decimal TaxableAmount { get; private set; } public decimal TaxAmount { get; private set; } public decimal FinalTotal { get; private set; }
    public static OrderItemPriceSnapshot Create(Guid company, Guid snapshot, Guid item, decimal raw, decimal print, decimal services, decimal discount, decimal taxRate, bool exempt) { decimal sub = Money.Round(raw + print + services); decimal d = Math.Min(Money.Round(discount), sub); decimal taxable = Money.Round(sub - d); decimal tax = exempt ? 0 : Money.Round(taxable * taxRate / 100m); return new() { Id = Guid.NewGuid(), CompanyId = company, OrderPriceSnapshotId = snapshot, OrderItemId = item, RawProductTotal = Money.Round(raw), PrintTotal = Money.Round(print), AdditionalServiceTotal = Money.Round(services), Subtotal = sub, DiscountAllocated = d, TaxableAmount = taxable, TaxAmount = tax, FinalTotal = Money.Round(taxable + tax) }; }
}

public sealed class PriceComponentSnapshot
{
    private PriceComponentSnapshot() { }
    public Guid Id { get; private set; } public Guid CompanyId { get; private set; } public Guid OrderPriceSnapshotId { get; private set; } public Guid? OrderItemId { get; private set; } public Guid? OrderPrintOperationId { get; private set; } public Guid? TariffId { get; private set; } public Guid? PricingRuleId { get; private set; } public long? PricingRuleVersion { get; private set; } public PriceComponentType ComponentType { get; private set; } public string TariffCode { get; private set; } = string.Empty; public decimal UnitAmount { get; private set; } public decimal Quantity { get; private set; } public decimal TotalAmount { get; private set; }
    public static PriceComponentSnapshot Create(Guid company, Guid snapshot, Guid? item, Guid? print, Tariff? tariff, PriceComponentType type, string code, decimal unit, decimal quantity, decimal total, Guid? ruleId = null, long? ruleVersion = null) => new() { Id = Guid.NewGuid(), CompanyId = company, OrderPriceSnapshotId = snapshot, OrderItemId = item, OrderPrintOperationId = print, TariffId = tariff?.Id, PricingRuleId = ruleId, PricingRuleVersion = ruleVersion, ComponentType = type, TariffCode = Tariff.Normalize(code), UnitAmount = Money.Round(unit), Quantity = quantity, TotalAmount = Money.Round(total) };
}
