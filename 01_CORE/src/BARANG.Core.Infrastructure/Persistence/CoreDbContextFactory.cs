using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CoreDatabase")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings__CoreDatabase before running EF Core migration commands.");

        DbContextOptionsBuilder<CoreDbContext> builder = new();
        builder.UseCorePostgreSql(connectionString);
        return new CoreDbContext(builder.Options);
    }
}
