using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260908140000_SequentialPersonnelCodes")]
public sealed class SequentialPersonnelCodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE barang.persons DISABLE ROW LEVEL SECURITY;

            UPDATE barang.persons
            SET personnel_code = 'migration-' || id::text;

            WITH ranked AS
            (
                SELECT id,
                       row_number() OVER
                       (
                           PARTITION BY company_id
                           ORDER BY created_at_utc, id
                       ) AS sequential_code
                FROM barang.persons
            )
            UPDATE barang.persons AS person
            SET personnel_code = ranked.sequential_code::text
            FROM ranked
            WHERE ranked.id = person.id;

            ALTER TABLE barang.persons ENABLE ROW LEVEL SECURITY;
            ALTER TABLE barang.persons FORCE ROW LEVEL SECURITY;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Sequential codes are permanent business identifiers; the original technical
        // values cannot be reconstructed safely and are intentionally not restored.
    }
}
