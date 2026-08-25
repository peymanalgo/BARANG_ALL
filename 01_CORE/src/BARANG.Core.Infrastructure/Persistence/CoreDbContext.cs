using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Persistence;

public sealed class CoreDbContext(DbContextOptions<CoreDbContext> options) : DbContext(options);
