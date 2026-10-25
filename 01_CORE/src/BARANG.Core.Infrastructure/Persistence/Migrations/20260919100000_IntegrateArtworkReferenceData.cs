using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260919100000_IntegrateArtworkReferenceData")]
public partial class IntegrateArtworkReferenceData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string[] columns = ["subcategory_id", "type_id", "style_id", "theme_id", "audience_id", "license_id", "usage_id"];
        foreach (string column in columns)
            migrationBuilder.AddColumn<Guid>(name: column, schema: "barang", table: "artworks", type: "uuid", nullable: true);

        foreach (string column in columns)
        {
            migrationBuilder.CreateIndex($"ix_artworks_{column}", "artworks", column, "barang");
            migrationBuilder.AddForeignKey(name: $"FK_artworks_lookup_values_{column}", schema: "barang", table: "artworks", column: column, principalSchema: "barang", principalTable: "lookup_values", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }

        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION barang.seed_extended_artwork_catalogs(target_company uuid) RETURNS void
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
            BEGIN
              INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
              SELECT gen_random_uuid(),target_company,d.code,d.caption,'Company',false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
              FROM (VALUES
                ('artwork.subcategory','Design Subcategories'),('artwork.type','Design Types'),
                ('artwork.style','Design Styles'),('artwork.theme','Design Themes'),
                ('artwork.audience','Design Audiences'),('artwork.license','Design License Types'),
                ('artwork.usage','Design Usage Types')) d(code,caption)
              WHERE NOT EXISTS (SELECT 1 FROM barang.lookup_types t WHERE t.company_id=target_company AND t.code=d.code);
            END;
            $fn$;
            REVOKE ALL ON FUNCTION barang.seed_extended_artwork_catalogs(uuid) FROM PUBLIC;
            DO $backfill$
            DECLARE company_row record;
            BEGIN
              FOR company_row IN SELECT id FROM barang.companies LOOP
                PERFORM barang.seed_extended_artwork_catalogs(company_row.id);
              END LOOP;
            END;
            $backfill$;
            CREATE OR REPLACE FUNCTION barang.company_extended_artwork_catalogs_created() RETURNS trigger
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
            BEGIN PERFORM barang.seed_extended_artwork_catalogs(NEW.id); RETURN NEW; END;
            $fn$;
            CREATE TRIGGER company_extended_artwork_catalogs_created AFTER INSERT ON barang.companies
            FOR EACH ROW EXECUTE FUNCTION barang.company_extended_artwork_catalogs_created();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS company_extended_artwork_catalogs_created ON barang.companies; DROP FUNCTION IF EXISTS barang.company_extended_artwork_catalogs_created(); DROP FUNCTION IF EXISTS barang.seed_extended_artwork_catalogs(uuid);");
        string[] columns = ["subcategory_id", "type_id", "style_id", "theme_id", "audience_id", "license_id", "usage_id"];
        foreach (string column in columns)
        {
            migrationBuilder.DropForeignKey(name: $"FK_artworks_lookup_values_{column}", schema: "barang", table: "artworks");
            migrationBuilder.DropIndex(name: $"ix_artworks_{column}", schema: "barang", table: "artworks");
            migrationBuilder.DropColumn(name: column, schema: "barang", table: "artworks");
        }
    }
}
