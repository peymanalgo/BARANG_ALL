using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260908080000_AllowLeafOrganizationDeletes")]
public sealed class AllowLeafOrganizationDeletes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $grant$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT DELETE ON barang.organizational_assignments TO barang_app;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    GRANT DELETE ON barang.organizational_assignments TO barang_test_app;
                END IF;
            END $grant$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $revoke$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    REVOKE DELETE ON barang.organizational_assignments FROM barang_app;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    REVOKE DELETE ON barang.organizational_assignments FROM barang_test_app;
                END IF;
            END $revoke$;
            """);
    }
}
