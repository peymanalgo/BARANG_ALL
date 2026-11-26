using BARANG.Core.Domain.Logistics;using Xunit;
namespace BARANG.Core.UnitTests;
#pragma warning disable CA1707
public sealed class LogisticsFoundationTests
{private static readonly Guid Company=Guid.NewGuid(),Order=Guid.NewGuid(),User=Guid.NewGuid();private static readonly DateTimeOffset Now=new(2026,9,1,12,0,0,TimeSpan.Zero);
[Fact]public void Package_requires_physical_metadata(){Assert.True(Package.Create(Company,Order,"PKG-1","BOX",0,1,1,1,Now).IsFailure);}
[Fact]public void Package_number_is_normalized_and_immutable(){Package p=Pack();Assert.Equal("PKG-1",p.PackageNumber);Assert.True(p.Complete(User,1,Now).IsSuccess);Assert.Equal("PKG-1",p.PackageNumber);}
[Fact]public void Package_completion_is_idempotent_and_stale_is_conflict(){Package p=Pack();Assert.Equal(LogisticsErrors.Concurrency.Code,p.Complete(User,2,Now).Error.Code);p.Complete(User,1,Now);Assert.True(p.Complete(User,1,Now).IsSuccess);}
[Fact]public void Package_item_requires_positive_quantity(){Assert.True(PackageItem.Create(Company,Guid.NewGuid(),Order,Guid.NewGuid(),0,Now).IsFailure);}
[Fact]public void Shipment_lifecycle_is_centralized(){Shipment s=Ship();Assert.True(s.InTransit(1,Now).IsFailure);s.Ship(1,Now);s.InTransit(2,Now);Assert.True(s.Deliver(3,Now).IsSuccess);Assert.True(s.Fail("X","late",4,Now).IsFailure);}
[Fact]public void Failed_delivery_requires_reason(){Shipment s=Ship();s.Ship(1,Now);Assert.True(s.Fail("","",2,Now).IsFailure);}
[Fact]public void Failed_delivery_can_return_to_returns_warehouse(){Shipment s=Ship();s.Ship(1,Now);s.Fail("NO_RECIPIENT","Absent",2,Now);Guid w=Guid.NewGuid();Assert.True(s.Return(w,"RETURN","Return to warehouse",3,Now).IsSuccess);Assert.Equal(w,s.ReturnWarehouseId);}
[Fact]public void Tracking_is_normalized_and_concurrency_protected(){Shipment s=Ship();Assert.True(s.RegisterTracking(" track-123 ",1,Now).IsSuccess);Assert.Equal("TRACK-123",s.TrackingCode);Assert.Equal(LogisticsErrors.Concurrency.Code,s.RegisterTracking("OTHER",1,Now).Error.Code);}
[Fact]public void Customer_return_requires_traceable_identity_and_is_idempotency_ready(){Guid shipment=Guid.NewGuid(),item=Guid.NewGuid();var created=ShipmentReturn.Create(Company,shipment,Order,item,2,"PCS","DAMAGED","Customer return","RET-1",User,Now);Assert.True(created.IsSuccess);Assert.Equal("RET-1",created.Value.IdempotencyKey);Assert.Equal(ShipmentReturnStatus.Posted,created.Value.Status);Assert.True(ShipmentReturn.Create(Guid.Empty,shipment,Order,item,1,"PCS","X","bad","RET-2",User,Now).IsFailure);}
private static Package Pack()=>Package.Create(Company,Order,"pkg-1","box",1,10,20,30,Now).Value;private static Shipment Ship()=>Shipment.Create(Company,Order,"shp-1","local","standard",null,Now).Value;}
#pragma warning restore CA1707
