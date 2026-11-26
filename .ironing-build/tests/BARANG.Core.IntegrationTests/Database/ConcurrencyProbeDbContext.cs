using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class ConcurrencyProbeDbContext(DbContextOptions<ConcurrencyProbeDbContext> options)
    : DbContext(options)
{
    public DbSet<ConcurrencyProbe> ConcurrencyProbes => Set<ConcurrencyProbe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConcurrencyProbe>(entity =>
        {
            entity.ToTable("concurrency_probes", "barang_test");
            entity.HasKey(probe => probe.Id).HasName("pk_concurrency_probes");
            entity.Property(probe => probe.Id).HasColumnName("id");
            entity.Property(probe => probe.Value).HasColumnName("value");
            entity.Property(probe => probe.Version)
                .HasColumnName("version")
                .IsConcurrencyToken();
        });
    }
}
