using System.Text.Json;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
namespace BARANG.Core.Infrastructure.Inventory;
public sealed partial class InventoryAdministration
{
 public Task<Result<ReserveOrderInventoryResult>> ReserveOrderAsync(ReserveOrderInventoryCommand command,CancellationToken cancellationToken)=>tx.ExecuteAsync(t=>ReserveOrderInsideTransactionAsync(command,t),cancellationToken);
 public async Task<Result<ReserveOrderInventoryResult>> ReserveOrderInsideTransactionAsync(ReserveOrderInventoryCommand command,CancellationToken cancellationToken)
 {
  ReserveOrderInventoryCommand c=command; CancellationToken ct=cancellationToken;
  if(c.OrderId==Guid.Empty||c.WarehouseId==Guid.Empty||string.IsNullOrWhiteSpace(c.IdempotencyKey))return Result.Failure<ReserveOrderInventoryResult>(InventoryErrors.InvalidData);
  if(!await db.Warehouses.AnyAsync(x=>x.CompanyId==Company&&x.Id==c.WarehouseId&&x.IsActive,ct))return Result.Failure<ReserveOrderInventoryResult>(Error.NotFound("inventory.warehouse.not_eligible"));
  var items=await db.OrderItems.Where(x=>x.CompanyId==Company&&x.OrderId==c.OrderId&&x.IsActive).OrderBy(x=>x.Id).ToListAsync(ct);if(items.Count==0)return Result.Failure<ReserveOrderInventoryResult>(Error.NotFound("order.not_found"));
  string prefix=c.IdempotencyKey.Trim()+":";var existing=await db.StockReservations.Where(x=>x.CompanyId==Company&&x.IdempotencyKey.StartsWith(prefix)).OrderBy(x=>x.OrderItemId).ToListAsync(ct);if(existing.Count>0){if(existing.Count!=items.Count||existing.Any(x=>x.WarehouseId!=c.WarehouseId)||items.Any(i=>existing.SingleOrDefault(x=>x.OrderItemId==i.Id) is not{ } x||x.RequestedQuantity!=i.Quantity||x.ProductVariantId!=i.ProductVariantId))return Result.Failure<ReserveOrderInventoryResult>(Error.Conflict("inventory.reservation.request_mismatch"));return Result.Success(new ReserveOrderInventoryResult(c.OrderId,c.WarehouseId,c.IdempotencyKey,existing.Select(ReservationView).ToArray(),[]));}
  var prepared=new List<(Guid Item,Guid Variant,string Uom,decimal Quantity)>();foreach(var item in items){if(item.ProductVariantId==Guid.Empty||!await db.ProductVariants.AnyAsync(x=>x.CompanyId==Company&&x.Id==item.ProductVariantId&&x.IsActive,ct))return Result.Failure<ReserveOrderInventoryResult>(Error.NotFound("inventory.variant.not_found"));prepared.Add((item.Id,item.ProductVariantId,await Uom(item.ProductVariantId,ct),item.Quantity));}
  var demand=prepared.GroupBy(x=>new{x.Variant,x.Uom}).OrderBy(x=>x.Key.Variant).ToArray();var locked=new Dictionary<(Guid Variant,string Uom),StockBalance>();foreach(var group in demand){StockBalance?b=await LockBalance(c.WarehouseId,group.Key.Variant,ct);if(b is null){b=StockBalance.Create(Company,c.WarehouseId,group.Key.Variant,Now);db.Add(b);}locked[(group.Key.Variant,group.Key.Uom)]=b;}
  var shortages=new List<OrderReservationShortage>();foreach(var group in demand){decimal required=group.Sum(x=>x.Quantity),available=locked[(group.Key.Variant,group.Key.Uom)].Available;if(available<required)foreach(var item in group)shortages.Add(new(item.Item,item.Variant,item.Uom,item.Quantity,Math.Max(0,available),Math.Max(0,item.Quantity-Math.Max(0,available))));}if(shortages.Count>0)return Result.Failure<ReserveOrderInventoryResult>(Error.BusinessRule("inventory.reservation.shortage",JsonSerializer.Serialize(shortages)));
  foreach(var group in demand){Result reserved=locked[(group.Key.Variant,group.Key.Uom)].Reserve(group.Sum(x=>x.Quantity),Now);if(reserved.IsFailure)return Result.Failure<ReserveOrderInventoryResult>(reserved.Error);}var reservations=new List<ReservationView>();foreach(var item in prepared){StockReservation r=StockReservation.Create(Company,item.Item,c.WarehouseId,item.Variant,item.Uom,item.Quantity,c.ExpiresAtUtc,$"{c.IdempotencyKey.Trim()}:{item.Item:N}",Now);db.Add(r);reservations.Add(ReservationView(r));Log("inventory.reservation.created",r.Id);}return Result.Success(new ReserveOrderInventoryResult(c.OrderId,c.WarehouseId,c.IdempotencyKey,reservations,[]));
 }
}
