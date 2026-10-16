using BARANG.Core.Domain.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BARANG.Core.Infrastructure.Persistence.Configurations;

internal sealed class QCInspectionDispositionConfiguration : IEntityTypeConfiguration<QCInspectionDisposition>
{
    public void Configure(EntityTypeBuilder<QCInspectionDisposition> b)
    {
        b.ToTable("qc_inspection_dispositions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CompanyId).HasColumnName("company_id");
        b.Property(x => x.InspectionId).HasColumnName("inspection_id");
        b.Property(x => x.InspectableQuantity).HasColumnName("inspectable_quantity").HasPrecision(18, 4);
        b.Property(x => x.UomCode).HasColumnName("uom_code").HasMaxLength(30);
        b.Property(x => x.AcceptedQuantity).HasColumnName("accepted_quantity").HasPrecision(18, 4);
        b.Property(x => x.RejectedQuantity).HasColumnName("rejected_quantity").HasPrecision(18, 4);
        b.Property(x => x.ReproductionRequiredQuantity).HasColumnName("reproduction_required_quantity").HasPrecision(18, 4);
        b.Property(x => x.AcceptedReason).HasColumnName("accepted_reason").HasMaxLength(500);
        b.Property(x => x.RejectedReason).HasColumnName("rejected_reason").HasMaxLength(500);
        b.Property(x => x.ReproductionReason).HasColumnName("reproduction_reason").HasMaxLength(500);
        b.Property(x => x.RecordedByUserId).HasColumnName("recorded_by_user_id");
        b.Property(x => x.RecordedAtUtc).HasColumnName("recorded_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.HasIndex(x => new { x.CompanyId, x.InspectionId }).IsUnique();
        b.HasOne<QCInspection>().WithMany().HasForeignKey(x => new { x.CompanyId, x.InspectionId })
            .HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
