using BARANG.Core.Domain.Inventory;
using Xunit;

namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707,CA1305
public sealed class InventoryFoundationTests
{
 private static readonly Guid Company=Guid.NewGuid(),Warehouse=Guid.NewGuid(),Variant=Guid.NewGuid(),Item=Guid.NewGuid(),Actor=Guid.NewGuid();private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-09-01T10:00:00Z");
 [Fact]public void Ledger_rejects_zero_quantity(){Assert.True(StockTransaction.Create(Company,Warehouse,Variant,StockTransactionType.Receipt,0,"EA","MANUAL",null,"R1",null,Actor,"c",Now,Now).IsFailure);}
 [Fact]public void Ledger_normalizes_technical_fields(){StockTransaction x=StockTransaction.Create(Company,Warehouse,Variant,StockTransactionType.Receipt,4," ea "," manual ",null," ref-1 ",null,Actor,"c",Now,Now).Value;Assert.Equal("EA",x.UomCode);Assert.Equal("REF-1",x.Reference);}
 [Fact]public void Balance_formula_is_exact(){StockBalance b=StockBalance.Create(Company,Warehouse,Variant,Now);Assert.True(b.ApplyOnHand(10,false,Now).IsSuccess);Assert.True(b.Reserve(4,Now).IsSuccess);Assert.Equal(10,b.OnHand);Assert.Equal(4,b.Reserved);Assert.Equal(6,b.Available);}
 [Fact]public void Balance_prevents_over_reservation(){StockBalance b=StockBalance.Create(Company,Warehouse,Variant,Now);b.ApplyOnHand(2,false,Now);Assert.Equal(InventoryErrors.Insufficient.Code,b.Reserve(3,Now).Error.Code);}
 [Fact]public void Balance_consume_updates_on_hand_and_reserved(){StockBalance b=StockBalance.Create(Company,Warehouse,Variant,Now);b.ApplyOnHand(10,false,Now);b.Reserve(4,Now);Assert.True(b.Consume(3,Now).IsSuccess);Assert.Equal(7,b.OnHand);Assert.Equal(1,b.Reserved);}
 [Fact]public void Reservation_supports_partial_release(){StockReservation r=StockReservation.Create(Company,Item,Warehouse,Variant,5,Now.AddHours(1),"key",Now);Assert.True(r.Release(2,1,Now).IsSuccess);Assert.Equal(3,r.RemainingQuantity);Assert.Equal(ReservationStatus.PartiallyReleased,r.Status);}
 [Fact]public void Reservation_rejects_over_release(){StockReservation r=StockReservation.Create(Company,Item,Warehouse,Variant,5,null,"key",Now);Assert.True(r.Release(6,1,Now).IsFailure);}
 [Fact]public void Reservation_detects_stale_version(){StockReservation r=StockReservation.Create(Company,Item,Warehouse,Variant,5,null,"key",Now);Assert.Equal(InventoryErrors.Concurrency.Code,r.Release(1,2,Now).Error.Code);}
 [Fact]public void Reservation_expiry_is_idempotent(){StockReservation r=StockReservation.Create(Company,Item,Warehouse,Variant,5,Now.AddMinutes(-1),"key",Now.AddHours(-1));Assert.Equal(5,r.Expire(Now));Assert.Equal(0,r.Expire(Now));Assert.Equal(ReservationStatus.Expired,r.Status);}
 [Fact]public void Reservation_keeps_exact_order_item_and_canonical_unit(){Guid secondItem=Guid.NewGuid();StockReservation first=StockReservation.Create(Company,Item,Warehouse,Variant,"KG",1.25m,null,"key-1",Now);StockReservation second=StockReservation.Create(Company,secondItem,Warehouse,Variant,"KG",1.25m,null,"key-2",Now);Assert.NotEqual(first.Id,second.Id);Assert.Equal(Item,first.OrderItemId);Assert.Equal(secondItem,second.OrderItemId);Assert.Equal("KG",first.UomCode);Assert.Equal(1.25m,first.RequestedQuantity);}
 [Fact]public void Purchase_lines_keep_unit_and_decimal_quantity(){PurchaseRequestItem request=PurchaseRequestItem.Create(Company,Guid.NewGuid(),Variant,"M2",2.375m);PurchaseOrderItem order=PurchaseOrderItem.Create(Company,Guid.NewGuid(),Variant,"M2",2.375m,10.125m);Assert.Equal("M2",request.UomCode);Assert.Equal(2.375m,request.RequestedQuantity);Assert.Equal("M2",order.UomCode);Assert.Equal(2.375m,order.OrderedQuantity);}
 [Fact]public void Transfer_requires_different_warehouses(){Assert.True(StockTransfer.Create(Company,Warehouse,Warehouse,Variant,1,"EA","T1",Now).IsFailure);}
 [Fact]public void Transfer_completion_is_concurrency_protected(){StockTransfer t=StockTransfer.Create(Company,Warehouse,Guid.NewGuid(),Variant,1,"EA","T1",Now).Value;Assert.True(t.Complete(2,Now).IsFailure);Assert.True(t.Complete(1,Now).IsSuccess);Assert.Equal(StockTransferStatus.Completed,t.Status);}
 [Fact]public void Purchase_order_transition_and_cancellation_reason_are_controlled(){PurchaseOrder p=PurchaseOrder.Create(Company,Guid.NewGuid(),Warehouse,"PO-1","IRR",null,Now);Assert.True(p.Close(true,null,1,Now).IsFailure);Assert.True(p.Submit(1,Now).IsSuccess);Assert.True(p.Close(true,"supplier unavailable",2,Now).IsSuccess);Assert.Equal(PurchaseOrderStatus.Cancelled,p.Status);}
}
#pragma warning restore CA1707,CA1305
