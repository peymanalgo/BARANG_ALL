using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260919101000_RemoveUnconsumedEquipmentCatalogs")]
public partial class RemoveUnconsumedEquipmentCatalogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $cleanup$
            DECLARE remaining bigint;
            BEGIN
              SELECT COUNT(*) INTO remaining
              FROM barang.lookup_values v
              JOIN barang.lookup_types t ON t.id = v.lookup_type_id
              WHERE t.code IN ('equipment.maintenance.strategy','equipment.spare.category','equipment.warranty.type','equipment.location.type');
              IF remaining <> 0 THEN
                RAISE EXCEPTION 'Cannot remove equipment catalogs with existing values: %', remaining;
              END IF;
              DELETE FROM barang.lookup_types
              WHERE code IN ('equipment.maintenance.strategy','equipment.spare.category','equipment.warranty.type','equipment.location.type');
            END $cleanup$;

            CREATE OR REPLACE FUNCTION barang.equipment_reference_catalogs_created() RETURNS trigger
            LANGUAGE plpgsql SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
            BEGIN
              INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
              SELECT gen_random_uuid(),NEW.id,x,x,'Company',false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
              FROM unnest(ARRAY[
                'equipment.category','equipment.subtype','equipment.brand','equipment.manufacturer',
                'equipment.condition','equipment.ownership','equipment.acquisition','equipment.criticality',
                'equipment.service.type','equipment.failure.type','equipment.failure.cause','equipment.repair.type',
                'equipment.inspection.type','equipment.calibration.type','equipment.retirement.reason'
              ]) x ON CONFLICT DO NOTHING;
              RETURN NEW;
            END $fn$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Removed catalogs had no values; Down restores only their empty type rows.
            INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
            SELECT gen_random_uuid(),c.id,x,x,'Company',false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
            FROM barang.companies c
            CROSS JOIN unnest(ARRAY['equipment.maintenance.strategy','equipment.spare.category','equipment.warranty.type','equipment.location.type']) x
            ON CONFLICT DO NOTHING;
        """);
    }
}
