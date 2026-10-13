using BARANG.Core.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

// The equipment detail columns were added by the existing equipment-module
// migrations. Keep their explicit snake_case mapping separate from the
// original catalog mapping so legacy equipment columns remain unchanged.
internal sealed class EquipmentExtendedConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> b)
    {
        b.Property(x => x.AssetNumber).HasColumnName("asset_number").HasMaxLength(80);
        b.Property(x => x.AssetTag).HasColumnName("asset_tag").HasMaxLength(80);
        b.Property(x => x.EquipmentCategoryCode).HasColumnName("category_code").HasMaxLength(80);
        b.Property(x => x.EquipmentSubtypeCode).HasColumnName("subtype_code").HasMaxLength(80);
        b.Property(x => x.BrandCode).HasColumnName("brand_code").HasMaxLength(80);
        b.Property(x => x.ManufacturerCode).HasColumnName("manufacturer_code").HasMaxLength(80);
        b.Property(x => x.ConditionCode).HasColumnName("condition_code").HasMaxLength(80);
        b.Property(x => x.CriticalityCode).HasColumnName("criticality_code").HasMaxLength(80);
        b.Property(x => x.OwnershipTypeCode).HasColumnName("ownership_type_code").HasMaxLength(80);
        b.Property(x => x.AcquisitionMethodCode).HasColumnName("acquisition_method_code").HasMaxLength(80);
        b.Property(x => x.LifecycleStatusCode).HasColumnName("lifecycle_status_code").HasMaxLength(80);
        b.Property(x => x.PhysicalLocation).HasColumnName("physical_location").HasMaxLength(250);
        b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        b.Property(x => x.ResponsiblePersonId).HasColumnName("responsible_person_id");
        b.Property(x => x.AssignedPersonId).HasColumnName("assigned_person_id");
        b.Property(x => x.PurchaseDate).HasColumnName("purchase_date");
        b.Property(x => x.PurchasePrice).HasColumnName("purchase_price").HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10);
        b.Property(x => x.SupplierId).HasColumnName("supplier_id");
        b.Property(x => x.PurchaseReference).HasColumnName("purchase_reference").HasMaxLength(120);
        b.Property(x => x.CommissioningDate).HasColumnName("commissioning_date");
        b.Property(x => x.WarrantyStart).HasColumnName("warranty_start");
        b.Property(x => x.WarrantyEnd).HasColumnName("warranty_end");
        b.Property(x => x.WarrantyProvider).HasColumnName("warranty_provider").HasMaxLength(200);
        b.Property(x => x.WarrantyNotes).HasColumnName("warranty_notes").HasMaxLength(1000);
        b.Property(x => x.ExpectedUsefulLifeMonths).HasColumnName("expected_useful_life_months");
        b.Property(x => x.RetirementDate).HasColumnName("retirement_date");
        b.Property(x => x.RetirementReasonCode).HasColumnName("retirement_reason_code").HasMaxLength(80);
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
    }
}
