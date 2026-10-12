using BARANG.Core.Application.Pagination;
using BARANG.Core.Domain.Catalog;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Application.Catalog;

public sealed record SourcingCommand<T>(T Data,long Version=0,long ParentVersion=0);
public sealed record SourcingQuery(string? Search=null,int PageNumber=1,int PageSize=10,string? SortBy=null,bool SortDescending=false,
    Guid? SupplierProductId=null,bool ActiveOnly=false);
public sealed record SupplierPriceView(SupplierProductPrice Price,string ProductName,string ProductCode,string? Sku,decimal NetUnitCost,bool IsCurrent);
public sealed record SupplierSourcingView(SupplierProduct Relationship,string ProductName,string ProductCode,string? Sku,IReadOnlyList<SupplierPriceView> CurrentPrices);
public sealed record SupplierPriceListView(SupplierPriceList PriceList,int LineCount);
public sealed record SupplierPriceListLineView(SupplierPriceListLine Line,string ProductName,string ProductCode,string? Sku,string? SupplierItemCode,decimal NetUnitCost);
public sealed record SupplierSourceQuote(Guid SupplierProductId,Guid SupplierId,string SupplierCode,string SupplierName,long SupplierNumber,string? SupplierItemCode,
    Guid ProductId,Guid? ProductVariantId,bool IsPreferred,Guid? PurchaseUnitId,decimal? MinimumOrderQuantity,decimal? MinimumOrderAmount,
    int? LeadTimeDays,string Availability,IReadOnlyList<SupplierPriceView> Prices);
public sealed record PurchasingSourcingQuery(Guid ProductId,Guid? ProductVariantId=null,decimal Quantity=1,DateOnly? AsOf=null,
    string? CurrencyCode=null,Guid? PurchaseUnitId=null);
public interface ISupplierSourcingAdministration
{
    Task<Result<PagedResult<SupplierSourcingView>>> ProductsAsync(Guid supplier,SourcingQuery query,CancellationToken ct);
    Task<Result<SupplierSourcingView>> ProductAsync(Guid supplier,Guid id,CancellationToken ct);
    Task<Result<SupplierSourcingView>> SaveProductAsync(Guid supplier,Guid? id,SourcingCommand<SupplierSourcingData> command,CancellationToken ct);
    Task<Result<SupplierSourcingView>> ProductStateAsync(Guid supplier,Guid id,long version,bool? active,bool? preferred,CancellationToken ct);
    Task<Result<bool>> DeleteProductAsync(Guid supplier,Guid id,long version,CancellationToken ct);
    Task<Result<PagedResult<SupplierPriceView>>> PricesAsync(Guid supplier,SourcingQuery query,CancellationToken ct);
    Task<Result<SupplierPriceView>> PriceAsync(Guid supplier,Guid id,CancellationToken ct);
    Task<Result<SupplierPriceView>> SavePriceAsync(Guid supplier,Guid? id,SourcingCommand<SupplierPriceData> command,CancellationToken ct);
    Task<Result<SupplierPriceView>> PriceStateAsync(Guid supplier,Guid id,long version,bool? active,DateOnly? endDate,CancellationToken ct);
    Task<Result<bool>> DeletePriceAsync(Guid supplier,Guid id,long version,CancellationToken ct);
    Task<Result<PagedResult<SupplierPriceListView>>> ListsAsync(Guid supplier,SourcingQuery query,CancellationToken ct);
    Task<Result<SupplierPriceListView>> ListAsync(Guid supplier,Guid id,CancellationToken ct);
    Task<Result<SupplierPriceListView>> SaveListAsync(Guid supplier,Guid? id,SourcingCommand<SupplierPriceListData> command,CancellationToken ct);
    Task<Result<SupplierPriceListView>> ListStateAsync(Guid supplier,Guid id,long version,bool active,CancellationToken ct);
    Task<Result<bool>> DeleteListAsync(Guid supplier,Guid id,long version,CancellationToken ct);
    Task<Result<PagedResult<SupplierPriceListLineView>>> LinesAsync(Guid supplier,Guid list,SourcingQuery query,CancellationToken ct);
    Task<Result<SupplierPriceListLineView>> LineAsync(Guid supplier,Guid list,Guid id,CancellationToken ct);
    Task<Result<SupplierPriceListLineView>> SaveLineAsync(Guid supplier,Guid list,Guid? id,SourcingCommand<SupplierPriceListLineData> command,CancellationToken ct);
    Task<Result<bool>> DeleteLineAsync(Guid supplier,Guid list,Guid id,long version,long parentVersion,CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierSourceQuote>>> SourcesAsync(PurchasingSourcingQuery query,CancellationToken ct);
    Task<Result<IReadOnlyList<SupplierSourceQuote>>> ProductSourcesAsync(Guid product,Guid? variant,CancellationToken ct);
}
