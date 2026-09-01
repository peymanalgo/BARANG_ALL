using BARANG.Core.Domain.Production;
using BARANG.Core.Domain.Quality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BARANG.Core.Infrastructure.Persistence.Configurations;
internal sealed class ProductionReproductionConfiguration:IEntityTypeConfiguration<ProductionJob>
{
 public void Configure(EntityTypeBuilder<ProductionJob>b)
 {
  b.HasOne<ReproductionRequest>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.ReproductionRequestId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<ProductionJob>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.SourceProductionJobId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<ProductionJob>().WithMany().HasForeignKey(x=>new{x.CompanyId,x.RootProductionJobId}).HasPrincipalKey(x=>new{x.CompanyId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasIndex(x=>new{x.CompanyId,x.ReproductionRequestId});
 }
}
