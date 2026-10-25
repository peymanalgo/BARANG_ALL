using BARANG.Core.Api.Errors;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using BARANG.Core.Domain.Security;
using BARANG.Core.Application.Organization;
using BARANG.Core.Infrastructure.Inventory;

namespace BARANG.Core.Api.Inventory;
public sealed record ReserveOrderInventoryRequest(Guid WarehouseId,string IdempotencyKey,DateTimeOffset? ExpiresAtUtc=null);
public sealed record AdjustOrderInventoryRequest(long OrderVersion,string RequestKey,IReadOnlyList<AdjustOrderItemCommand> Items);
public sealed record ReturnStockRequest(Guid OriginalIssueTransactionId,decimal Quantity,string Reference,string? UomCode=null,string? Disposition=null);
public static class InventoryEndpoints
{
 public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder e)
 {
  RouteGroupBuilder inventory=e.MapGroup("/api/v1/inventory");
  inventory.MapGet("/balances",async(Guid?warehouseId,Guid?variantId,IInventoryAdministration s,CancellationToken ct)=>Map(await s.BalancesAsync(warehouseId,variantId,ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/transactions",async(Guid?warehouseId,Guid?variantId,IInventoryAdministration s,CancellationToken ct)=>Map(await s.TransactionsAsync(warehouseId,variantId,ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/adjustments",async(string?search,Guid?warehouseId,int?pageNumber,int?pageSize,IInventoryAdministration s,CancellationToken ct)=>Map(await s.AdjustmentsAsync(search,warehouseId,Math.Max(1,pageNumber??1),Math.Clamp(pageSize??20,1,100),ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/returnable-issues",async(IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReturnableIssuesAsync(ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapGet("/shortages",async(bool?includeClosed,int?limit,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ShortagesAsync(includeClosed??false,Math.Clamp(limit??100,1,200),ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/reservations",async(bool?includeClosed,int?limit,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReservationsAsync(includeClosed??false,Math.Clamp(limit??100,1,200),ct))).RequireAuthorization(SecurityPermissions.InventoryRead);
  inventory.MapGet("/warehouses",async(IOrganizationAdministration s,CancellationToken ct)=>Map(await s.ListWarehousesAsync(ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapGet("/warehouses/{warehouseId:guid}/responsibilities",async(Guid warehouseId,IInventoryAdministration s,CancellationToken ct)=>Map(await s.WarehouseResponsibilitiesAsync(warehouseId,ct))).RequireAuthorization(SecurityPermissions.OrganizationWarehouseManage);
  inventory.MapPost("/warehouses/{warehouseId:guid}/responsibilities",async(Guid warehouseId,CreateWarehouseResponsibilityRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.AddWarehouseResponsibilityAsync(new(warehouseId,c.ApplicationUserId),ct),true)).RequireAuthorization(SecurityPermissions.OrganizationWarehouseManage);
  inventory.MapPut("/warehouse-responsibilities/{id:guid}/activation",async(Guid id,ActivationWarehouseResponsibilityRequest c,IInventoryAdministration s,CancellationToken ct)=>MapVoid(await s.SetWarehouseResponsibilityActiveAsync(id,c.IsActive,c.Version,ct))).RequireAuthorization(SecurityPermissions.OrganizationWarehouseManage);
  inventory.MapPost("/orders/{orderId:guid}/reservations",async(Guid orderId,ReserveOrderInventoryRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReserveOrderAsync(new(orderId,c.WarehouseId,c.IdempotencyKey,c.ExpiresAtUtc),ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/orders/{orderId:guid}/issues",async(Guid orderId,IssueOrderInventoryRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.IssueOrderAsync(new(orderId,c.WarehouseId,c.Reference,c.Items),ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/orders/{orderId:guid}/adjustments",async(Guid orderId,AdjustOrderInventoryRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.AdjustOrderAsync(new(orderId,c.OrderVersion,c.RequestKey,c.Items),ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/receipts",async(StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReceiveAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/issues/{type}",async(StockTransactionType type,StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.IssueAsync(c,type,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/adjustments/{increase:bool}",async(bool increase,StockMovementCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.AdjustAsync(c,increase,ct),true)).RequireAuthorization(SecurityPermissions.InventoryAdjust);
  inventory.MapPost("/reservations",async(ReserveStockCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReserveAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/{id:guid}/release",async(Guid id,ReleaseReservationCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReleaseAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/{id:guid}/issue",async(Guid id,IssueReservationCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.IssueReservedAsync(id,c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/returns",async(ReturnStockRequest c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.ReturnAsync(new(c.OriginalIssueTransactionId,c.Quantity,c.Reference,c.UomCode,c.Disposition),ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/reservations/expire",async(IInventoryAdministration s,CancellationToken ct)=>Map(await s.ExpireAsync(ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/transfers",async(TransferCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.TransferAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.InventoryManage);
  inventory.MapPost("/transfers/{id:guid}/complete",async(Guid id,CompleteTransferCommand c,IInventoryAdministration s,CancellationToken ct)=>Map(await s.CompleteTransferAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.InventoryManage);
  RouteGroupBuilder purchasing=e.MapGroup("/api/v1/purchasing");
  purchasing.MapGet("/requests",async(IPurchasingAdministration s,CancellationToken ct)=>Map(await s.ListRequestsAsync(ct))).RequireAuthorization(SecurityPermissions.PurchasingRead);
  purchasing.MapGet("/requests/{id:guid}",async(Guid id,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.GetRequestAsync(id,ct))).RequireAuthorization(SecurityPermissions.PurchasingRead);
  purchasing.MapPost("/requests",async(CreatePurchaseRequestCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CreateRequestAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/shortages/{shortageId:guid}/convert",async(Guid shortageId,InventoryAdministration s,CancellationToken ct)=>Map(await s.ConvertShortageToRequestAsync(shortageId,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/requests/{id:guid}/submit",async(Guid id,long version,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.SubmitRequestAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/requests/{id:guid}/approve",async(Guid id,PurchasingDecisionCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.DecideRequestAsync(id,true,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/requests/{id:guid}/reject",async(Guid id,PurchasingDecisionCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.DecideRequestAsync(id,false,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapGet("/orders",async(IPurchasingAdministration s,CancellationToken ct)=>Map(await s.ListOrdersAsync(ct))).RequireAuthorization(SecurityPermissions.PurchasingRead);
  purchasing.MapPost("/orders",async(CreatePurchaseOrderCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CreateOrderAsync(c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/submit",async(Guid id,long version,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.SubmitOrderAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/approve",async(Guid id,PurchasingDecisionCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.DecideOrderAsync(id,true,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/reject",async(Guid id,PurchasingDecisionCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.DecideOrderAsync(id,false,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/send",async(Guid id,long version,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.SendOrderAsync(id,version,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/receipts",async(Guid id,PurchaseReceiptCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.ReceiveAsync(id,c,ct),true)).RequireAuthorization(SecurityPermissions.PurchasingManage);
  purchasing.MapPost("/orders/{id:guid}/close",async(Guid id,ClosePurchaseOrderCommand c,IPurchasingAdministration s,CancellationToken ct)=>Map(await s.CloseAsync(id,c,ct))).RequireAuthorization(SecurityPermissions.PurchasingManage);
  return e;
 }
 private static EndpointResult<T>Map<T>(Result<T>r,bool created=false)=>new(r,created);private static EndpointVoidResult MapVoid(Result r)=>new(r);private sealed class EndpointResult<T>(Result<T>r,bool created):IResult{public Task ExecuteAsync(HttpContext c){if(r.IsSuccess){c.Response.StatusCode=created?201:200;return c.Response.WriteAsJsonAsync(r.Value,c.RequestAborted);}return Results.Problem(ApiProblemDetailsMapper.Create(r.Error,c.TraceIdentifier,c.Request.Path)).ExecuteAsync(c);}} private sealed class EndpointVoidResult(Result r):IResult{public Task ExecuteAsync(HttpContext c){if(r.IsSuccess){c.Response.StatusCode=204;return Task.CompletedTask;}return Results.Problem(ApiProblemDetailsMapper.Create(r.Error,c.TraceIdentifier,c.Request.Path)).ExecuteAsync(c);}}
}
public sealed record IssueOrderInventoryRequest(Guid WarehouseId,string Reference,IReadOnlyList<IssueOrderItemCommand> Items);
public sealed record CreateWarehouseResponsibilityRequest(Guid ApplicationUserId);
public sealed record ActivationWarehouseResponsibilityRequest(bool IsActive,long Version);
