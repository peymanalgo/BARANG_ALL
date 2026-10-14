using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Security;
using BARANG.Core.Application.Organization;

namespace BARANG.Core.Api.Inventory;
public sealed record ReserveOrderInventoryRequest(Guid WarehouseId,string IdempotencyKey,DateTimeOffset? ExpiresAtUtc=null);
public static class InventoryEndpoints
{
 public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder e)
 {
  RouteGroupBuilder inventory=e.MapGroup("/api/v1/inventory");
  inventory.MapGet("/balances",async(Guid?warehouseId,Guid?variantId,IInventoryAdministration s,CancellationToken ct)=>Map(await s.BalancesAsync(warehouseId,variantId,ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/transactions",async(Guid?warehouseId,Guid?variantId,IInventoryAdministration s,CancellationToken ct)=>Map(await s.TransactionsAsync(warehouseId,variantId,ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/shortages",async(bool?includeClosed,int?limit,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ShortagesAsync(includeClosed??false,Math.Clamp(limit??100,1,200),ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/reservations",async(bool?includeClosed,int?limit,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReservationsAsync(includeClosed??false,Math.Clamp(limit??100,1,200),ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/warehouses",async(IOrganizationAdministration s,CancellationToken ct)=>Map(await s.ListWarehousesAsync(ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/orders/{orderId:guid}/reservations",async(Guid orderId,ReserveOrderInventoryRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReserveOrderAsync(new(orderId,c.WarehouseId,c.IdempotencyKey,c.ExpiresAtUtc),ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/receipts",async(StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReceiveAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/issues/{type}",async(StockTransactionType type,StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.IssueAsync(c,type,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/adjustments/{increase:bool}",async(bool increase,StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.AdjustAsync(c,increase,ct),true)).RequireAuthorization(SecurityPermissions.InventoryAdjust);
  inventory.MapPost("/reservations",async(ReserveStockCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReserveAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/{id:guid}/release",async(Guid id,ReleaseReservationCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReleaseAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/{id:guid}/issue",async(Guid id,IssueReservationCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.IssueReservedAsync(id,c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/expire",async(IInventoryAdministration s,CancellationToken ct)=>Map(await s.ExpireAsync(ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/transfers",async(TransferCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.TransferAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/transfers/{id:guid}/complete",async(Guid id,CompleteTransferCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.CompleteTransferAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  RouteGroupBuilder purchasing=e.MapGroup("/api/v1/purchasing");
  purchasing.MapPost("/requests",async(CreatePurchaseRequestCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CreateRequestAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/requests/{id:guid}/submit",async(Guid id,long version,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.SubmitRequestAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapGet("/orders",async(IPurchasingAdministration s,CancellationToken ct)=>Map(await s.ListOrdersAsync(ct))).RequireAuthorization(SecurityPermissions.PurchasingRead);
  purchasing.MapPost("/orders",async(CreatePurchaseOrderCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CreateOrderAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/submit",async(Guid id,long version,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.SubmitOrderAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/receipts",async(Guid id,PurchaseReceiptCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.ReceiveAsync(id,c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/close",async(Guid id,ClosePurchaseOrderCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CloseAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  return e;
 }
 private static EndpointResult<T>Map<T>(Result<T>r,bool created=false)=>new(r,created);private sealed class EndpointResult<T>(Result<T>r,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(r.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(r.Value,c.RequestAborted);}return Results.Problem(ApiProblemDetailsMapper.Create(r.Error,c.TraceIdentifier,c.Request.Path)).ExecuteAsync(c);}}
}
