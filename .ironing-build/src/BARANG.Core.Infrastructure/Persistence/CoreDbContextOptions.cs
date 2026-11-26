using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public static class CoreDbContextOptions
{
    public static DbContextOptionsBuilder UseCorePostgreSql(
        this DbContextOptionsBuilder builder,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return builder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(CoreDbContext).Assembly.FullName);
            npgsql.MigrationsHistoryTable(
                CoreDbContext.MigrationsHistoryTable,
                CoreDbContext.DefaultSchema);
        });
    }
}
