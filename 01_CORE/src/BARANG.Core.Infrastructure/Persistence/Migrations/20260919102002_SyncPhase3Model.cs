using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPhase3Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                  IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'barang' AND table_name = 'tariffs' AND column_name = 'logical_tariff_id'
                  ) THEN
                    ALTER TABLE barang.tariffs ADD COLUMN logical_tariff_id uuid;
                  END IF;
                  IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'barang' AND table_name = 'tariffs' AND column_name = 'name'
                  ) THEN
                    ALTER TABLE barang.tariffs ADD COLUMN name character varying(200);
                  END IF;
                  IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'barang' AND table_name = 'tariffs' AND column_name = 'description'
                  ) THEN
                    ALTER TABLE barang.tariffs ADD COLUMN description character varying(2000);
                  END IF;
                  IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'barang' AND table_name = 'tariffs' AND column_name = 'version_number'
                  ) THEN
                    ALTER TABLE barang.tariffs ADD COLUMN version_number integer;
                  END IF;
                  UPDATE barang.tariffs
                  SET logical_tariff_id = COALESCE(logical_tariff_id, id),
                      name = COALESCE(name, code),
                      version_number = COALESCE(version_number, 1);
                  ALTER TABLE barang.tariffs ALTER COLUMN logical_tariff_id SET NOT NULL;
                  ALTER TABLE barang.tariffs ALTER COLUMN name SET NOT NULL;
                  ALTER TABLE barang.tariffs ALTER COLUMN version_number SET NOT NULL;
                  CREATE UNIQUE INDEX IF NOT EXISTS uq_tariffs_logical_version
                    ON barang.tariffs(company_id, logical_tariff_id, version_number);
                END $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_tariffs_logical_version",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "logical_tariff_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "version_number",
                schema: "barang",
                table: "tariffs");
        }
    }
}
