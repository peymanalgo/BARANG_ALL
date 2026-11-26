using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "artwork_translations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artwork_id = table.Column<Guid>(type: "uuid", nullable: false),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_translations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_artwork_translations_company_id",
                schema: "barang",
                table: "artwork_translations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_artwork_translations_artwork_culture",
                schema: "barang",
                table: "artwork_translations",
                columns: new[] { "artwork_id", "culture" },
                unique: true);
            // backfill: existing artwork texts become their fa translation (per-company GUC for RLS)
            migrationBuilder.Sql(@"DO $bf$
            DECLARE
                cid uuid;
            BEGIN
                FOR cid IN SELECT id FROM barang.companies LOOP
                    PERFORM set_config('barang.company_id', cid::text, false);
                    INSERT INTO barang.artwork_translations(id,company_id,artwork_id,culture,title,description,created_at_utc,updated_at_utc,version)
                    SELECT gen_random_uuid(),a.company_id,a.id,'fa',a.title,a.description,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                    FROM barang.artworks a
                    WHERE a.company_id=cid
                    AND NOT EXISTS(SELECT 1 FROM barang.artwork_translations x WHERE x.artwork_id=a.id AND x.culture='fa');
                END LOOP;
                PERFORM set_config('barang.company_id', '', false);
            END
            $bf$");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artwork_translations",
                schema: "barang");
        }
    }
}
