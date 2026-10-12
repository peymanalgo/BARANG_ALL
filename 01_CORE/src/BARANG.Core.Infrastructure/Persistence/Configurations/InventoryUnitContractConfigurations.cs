using BARANG.Core.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class StockReservationUnitContractConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder) => builder.Property(x => x.UomCode).HasColumnName("uom_code").HasMaxLength(50).IsRequired();
}

internal sealed class PurchaseRequestItemUnitContractConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder) => builder.Property(x => x.UomCode).HasColumnName("uom_code").HasMaxLength(50).IsRequired();
}

internal sealed class PurchaseOrderItemUnitContractConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder) => builder.Property(x => x.UomCode).HasColumnName("uom_code").HasMaxLength(50).IsRequired();
}
