using BARANG.Core.Domain.Orders;
using Xunit;
#pragma warning disable CA1707,CA1305

namespace BARANG.Core.UnitTests;
public sealed class OrderFoundationTests
{
 private static readonly Guid Company=Guid.NewGuid(),Branch=Guid.NewGuid(),Customer=Guid.NewGuid(),Actor=Guid.NewGuid();private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-08-31T12:00:00Z");
 [Fact]public void Draft_is_created_without_number(){var r=Order.CreateDraft(Guid.NewGuid(),Company,Branch,Customer,OrderSource.Internal,null,new DateOnly(2026,8,31),null,Actor,Now);Assert.True(r.IsSuccess);Assert.Equal(OrderStatus.Draft,r.Value.Status);Assert.Null(r.Value.OrderNumber);Assert.Equal(1,r.Value.Version);}
 [Fact]public void External_reference_is_bounded(){Assert.True(Order.CreateDraft(Guid.NewGuid(),Company,Branch,Customer,OrderSource.Web,new string('x',151),new(2026,8,31),null,Actor,Now).IsFailure);}
 [Fact]public void Draft_edit_uses_concurrency_version_and_status_guard(){var o=Draft();Assert.True(o.EditDraft(Branch,Customer,new(2026,9,1),new(2026,9,2),"WEB-1",Now.AddMinutes(1)).IsSuccess);Assert.Equal(2,o.Version);Assert.True(o.Submit("ORD-2026-0000000001",Now.AddMinutes(2)).IsSuccess);Assert.Equal(OrderErrors.NotDraft.Code,o.EditDraft(Branch,Customer,new(2026,9,1),null,null,Now).Error.Code);}
 [Fact]public void Submit_number_is_immutable(){var o=Draft();Assert.True(o.Submit("ORD-2026-0000000001",Now).IsSuccess);Assert.Equal(OrderStatus.Submitted,o.Status);Assert.Equal("ORD-2026-0000000001",o.OrderNumber);Assert.True(o.Submit("ORD-2026-0000000002",Now).IsFailure);Assert.Equal("ORD-2026-0000000001",o.OrderNumber);}
 [Fact]public void Cancellation_requires_reason_and_preserves_order(){var o=Draft();Assert.True(o.Cancel("",null,Actor,Now).IsFailure);Assert.True(o.Cancel("CUSTOMER_REQUEST","duplicate",Actor,Now).IsSuccess);Assert.Equal(OrderStatus.Cancelled,o.Status);Assert.NotNull(o.CancelledAtUtc);}
 [Fact]public void Item_requires_positive_quantity_and_removal_is_state_based(){Assert.True(OrderItem.Create(Guid.NewGuid(),Company,Guid.NewGuid(),1,Guid.NewGuid(),Guid.NewGuid(),0,null,Now).IsFailure);var i=OrderItem.Create(Guid.NewGuid(),Company,Guid.NewGuid(),1,Guid.NewGuid(),Guid.NewGuid(),2,null,Now).Value;i.Remove(Now);Assert.False(i.IsActive);Assert.Equal(2,i.Version);}
 [Fact]public void Product_snapshot_freezes_controlled_fields(){var i=OrderItem.Create(Guid.NewGuid(),Company,Guid.NewGuid(),1,Guid.NewGuid(),Guid.NewGuid(),2,null,Now).Value;i.Freeze("P1","Shirt","SKU1","Large blue","L","BLUE","PCS",Now);Assert.Equal("P1",i.SnapshotProductCode);Assert.Equal("SKU1",i.SnapshotVariantSku);Assert.Equal("PCS",i.SnapshotUomCode);}
 [Fact]public void Print_operation_validates_dimensions_and_can_be_state_removed(){Assert.True(OrderPrintOperation.Create(Guid.NewGuid(),Company,Guid.NewGuid(),Guid.NewGuid(),1,"FRONT",null,0,10,null,null,null,Now).IsFailure);var p=OrderPrintOperation.Create(Guid.NewGuid(),Company,Guid.NewGuid(),Guid.NewGuid(),1,"FRONT","SCREEN",10,20,null,null,null,Now).Value;p.Remove(Now);Assert.False(p.IsActive);}
 [Fact]public void Shipping_snapshot_uses_date_independent_value_data(){var r=OrderAddressSnapshot.Create(Guid.NewGuid(),Company,Guid.NewGuid(),null,"Receiver","+98912","IR","Tehran","Tehran",null,"Street",null,"123",Now);Assert.True(r.IsSuccess);Assert.Equal("IR",r.Value.CountryCode);Assert.True(r.Value.Update("Changed",null,"IR",null,"Qom",null,"New street",null,null,Now).IsSuccess);Assert.Equal(2,r.Value.Version);}
 [Fact]public void Change_request_is_bounded_and_never_executes_patch(){Assert.True(OrderChangeRequest.Create(Company,Guid.NewGuid(),Actor,"SHIPPING_ADDRESS","fix","{}",Now).IsSuccess);Assert.True(OrderChangeRequest.Create(Company,Guid.NewGuid(),Actor,"SHIPPING_ADDRESS","fix",new string('x',8001),Now).IsFailure);}
 private static Order Draft()=>Order.CreateDraft(Guid.NewGuid(),Company,Branch,Customer,OrderSource.Internal,null,new(2026,8,31),null,Actor,Now).Value;
}
#pragma warning restore CA1707,CA1305
