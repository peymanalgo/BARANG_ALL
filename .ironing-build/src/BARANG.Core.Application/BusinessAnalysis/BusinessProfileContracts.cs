using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.BusinessAnalysis;

public sealed record CustomerBusinessProfileCommand(DateOnly? DateOfBirth,string? EconomicCode,decimal CreditLimit,int PaymentTermsDays,string? BusinessDetails,long Version=0);
public sealed record ProductTechnicalProfileCommand(Guid? ProductVariantId,string? BrandCode,string? MaterialCode,string? GenderCode,string? FitCode,string? StyleCode,string? TechnicalAttributes,long Version=0);
public sealed record OrderBusinessProfileCommand(string? ShippingMethodCode,string? PackagingTypeCode,bool IncludeInvoiceInPackage,string? OrderTypeCode,string? Notes,long Version=0);
public sealed record PrintTechnicalProfileCommand(string? DirectionCode,string? PrintSizeCode,int PrintCount,Guid? PrinterEquipmentId,Guid? HeatPressEquipmentId,string? Specifications,long Version=0);
public sealed record CustomerBusinessProfileView(Guid Id,Guid CustomerId,DateOnly? DateOfBirth,string? EconomicCode,decimal CreditLimit,int PaymentTermsDays,string? BusinessDetails,long Version);
public sealed record ProductTechnicalProfileView(Guid Id,Guid ProductId,Guid? ProductVariantId,string? BrandCode,string? MaterialCode,string? GenderCode,string? FitCode,string? StyleCode,string? TechnicalAttributes,long Version);
public sealed record OrderBusinessProfileView(Guid Id,Guid OrderId,string? ShippingMethodCode,string? PackagingTypeCode,bool IncludeInvoiceInPackage,string? OrderTypeCode,string? Notes,long Version);
public sealed record PrintTechnicalProfileView(Guid Id,Guid OrderPrintOperationId,string? DirectionCode,string? PrintSizeCode,int PrintCount,Guid? PrinterEquipmentId,Guid? HeatPressEquipmentId,string? Specifications,long Version);

public interface IBusinessProfileAdministration
{
    Task<Result<CustomerBusinessProfileView>> GetCustomerAsync(Guid id,CancellationToken ct);
    Task<Result<CustomerBusinessProfileView>> SetCustomerAsync(Guid id,CustomerBusinessProfileCommand c,CancellationToken ct);
    Task<Result<ProductTechnicalProfileView>> GetProductAsync(Guid id,Guid? variant,CancellationToken ct);
    Task<Result<ProductTechnicalProfileView>> SetProductAsync(Guid id,ProductTechnicalProfileCommand c,CancellationToken ct);
    Task<Result<OrderBusinessProfileView>> GetOrderAsync(Guid id,CancellationToken ct);
    Task<Result<OrderBusinessProfileView>> SetOrderAsync(Guid id,OrderBusinessProfileCommand c,CancellationToken ct);
    Task<Result<PrintTechnicalProfileView>> GetPrintAsync(Guid id,CancellationToken ct);
    Task<Result<PrintTechnicalProfileView>> SetPrintAsync(Guid id,PrintTechnicalProfileCommand c,CancellationToken ct);
}
