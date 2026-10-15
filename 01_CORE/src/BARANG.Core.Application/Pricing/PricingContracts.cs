using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Orders;
using BARANG.Core.Domain.Pricing;

namespace BARANG.Core.Application.Pricing;

public sealed record TariffCommand(TariffType TariffType,string Code,Guid? ProductVariantId,string? PrintPositionCode,string? PrintMethodCode,decimal Amount,string CurrencyCode,DateOnly ValidFrom,DateOnly? ValidTo,long Version=0);
public sealed record TariffView(Guid Id,TariffType TariffType,string Code,Guid? ProductVariantId,string? PrintPositionCode,string? PrintMethodCode,decimal Amount,string CurrencyCode,DateOnly ValidFrom,DateOnly? ValidTo,bool IsActive,long Version);
public sealed record DiscountRuleCommand(string Code,DiscountType DiscountType,DiscountScope Scope,Guid? CustomerId,OrderSource? Source,decimal Value,string CurrencyCode,DateOnly ValidFrom,DateOnly? ValidTo);
public sealed record TaxRuleCommand(string Code,decimal RatePercent,bool IsExempt,DateOnly ValidFrom,DateOnly? ValidTo);
public sealed record ManualDiscountCommand(DiscountType Type,decimal Value,string? Reason);
public sealed record PriceComponentView(Guid Id,Guid? OrderItemId,Guid? OrderPrintOperationId,PriceComponentType Type,string TariffCode,decimal UnitAmount,decimal Quantity,decimal TotalAmount);
public sealed record OrderItemPricingView(Guid OrderItemId,decimal RawProductTotal,decimal PrintTotal,decimal AdditionalServiceTotal,decimal Subtotal,decimal DiscountAllocated,decimal TaxableAmount,decimal TaxAmount,decimal FinalTotal);
public sealed record OrderPricingView(Guid Id,Guid OrderId,string CurrencyCode,decimal RawProductTotal,decimal PrintTotal,decimal AdditionalServiceTotal,decimal PackagingTotal,decimal ShippingTotal,decimal Subtotal,decimal DiscountTotal,decimal TaxableTotal,decimal TaxTotal,decimal FinalTotal,decimal TaxRatePercent,bool IsTaxExempt,bool IsFinalized,DateTimeOffset CalculatedAtUtc,long Version,IReadOnlyList<OrderItemPricingView> Items,IReadOnlyList<PriceComponentView> Components);
public interface IOrderPricingEngine { Task<Result<OrderPricingView>> CalculateInsideTransactionAsync(Order order,bool finalize,ManualDiscountCommand? manual,CancellationToken cancellationToken,bool allowSubmittedRevision=false); }
public interface IPricingAdministration
{
    Task<Result<TariffView>> CreateTariffAsync(TariffCommand command,CancellationToken cancellationToken);
    Task<Result<TariffView>> UpdateTariffAsync(Guid id,TariffCommand command,bool active,CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<TariffView>>> ListTariffsAsync(TariffType? type,bool includeInactive,CancellationToken cancellationToken);
    Task<Result<Guid>> CreateDiscountRuleAsync(DiscountRuleCommand command,CancellationToken cancellationToken);
    Task<Result<Guid>> CreateTaxRuleAsync(TaxRuleCommand command,CancellationToken cancellationToken);
    Task<Result<OrderPricingView>> RecalculateAsync(Guid orderId,long orderVersion,ManualDiscountCommand? manual,CancellationToken cancellationToken);
    Task<Result<OrderPricingView>> GetOrderPricingAsync(Guid orderId,CancellationToken cancellationToken);
}
