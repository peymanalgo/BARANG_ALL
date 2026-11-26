using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignTariffCompatibilityColumns : Migration
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
                    WHERE table_schema = 'barang' AND table_name = 'tariffs' AND column_name = 'version_number'
                  ) THEN
                    ALTER TABLE barang.tariffs ADD COLUMN version_number integer;
                  END IF;
                  UPDATE barang.tariffs
                  SET logical_tariff_id = COALESCE(logical_tariff_id, id),
                      name = COALESCE(name, code),
                      version_number = COALESCE(version_number, 1);
                  ALTER TABLE barang.tariffs ALTER COLUMN logical_tariff_id SET DEFAULT gen_random_uuid();
                  ALTER TABLE barang.tariffs ALTER COLUMN name SET DEFAULT '';
                  ALTER TABLE barang.tariffs ALTER COLUMN version_number SET DEFAULT 1;
                  ALTER TABLE barang.tariffs ALTER COLUMN logical_tariff_id SET NOT NULL;
                  ALTER TABLE barang.tariffs ALTER COLUMN name SET NOT NULL;
                  ALTER TABLE barang.tariffs ALTER COLUMN version_number SET NOT NULL;
                END $migration$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE barang.tariffs ALTER COLUMN logical_tariff_id DROP DEFAULT;
                ALTER TABLE barang.tariffs ALTER COLUMN name DROP DEFAULT;
                ALTER TABLE barang.tariffs ALTER COLUMN version_number DROP DEFAULT;
                """);
        }
    }
}
