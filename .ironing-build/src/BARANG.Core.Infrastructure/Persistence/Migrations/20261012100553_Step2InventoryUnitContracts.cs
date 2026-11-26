using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Step2InventoryUnitContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uom_code",
                schema: "barang",
                table: "stock_reservations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uom_code",
                schema: "barang",
                table: "purchase_request_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uom_code",
                schema: "barang",
                table: "purchase_order_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE barang.stock_reservations r
                SET uom_code = p.unit_of_measure_code
                FROM barang.order_items oi
                JOIN barang.products p ON p.company_id = oi.company_id AND p.id = oi.product_id
                JOIN barang.product_variants pv ON pv.company_id = oi.company_id AND pv.product_id = oi.product_id AND pv.id = oi.product_variant_id
                WHERE r.company_id = oi.company_id AND r.order_item_id = oi.id AND r.product_variant_id = pv.id;

                UPDATE barang.purchase_request_items i
                SET uom_code = p.unit_of_measure_code
                FROM barang.product_variants pv
                JOIN barang.products p ON p.company_id = pv.company_id AND p.id = pv.product_id
                WHERE i.company_id = pv.company_id AND i.product_variant_id = pv.id;

                UPDATE barang.purchase_order_items i
                SET uom_code = p.unit_of_measure_code
                FROM barang.product_variants pv
                JOIN barang.products p ON p.company_id = pv.company_id AND p.id = pv.product_id
                WHERE i.company_id = pv.company_id AND i.product_variant_id = pv.id;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM barang.stock_reservations WHERE uom_code IS NULL)
                       OR EXISTS (SELECT 1 FROM barang.purchase_request_items WHERE uom_code IS NULL)
                       OR EXISTS (SELECT 1 FROM barang.purchase_order_items WHERE uom_code IS NULL) THEN
                        RAISE EXCEPTION 'STEP2 unresolved inventory unit backfill';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("ALTER TABLE barang.stock_reservations ALTER COLUMN uom_code SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE barang.purchase_request_items ALTER COLUMN uom_code SET NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE barang.purchase_order_items ALTER COLUMN uom_code SET NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uom_code",
                schema: "barang",
                table: "stock_reservations");

            migrationBuilder.DropColumn(
                name: "uom_code",
                schema: "barang",
                table: "purchase_request_items");

            migrationBuilder.DropColumn(
                name: "uom_code",
                schema: "barang",
                table: "purchase_order_items");
        }
    }
}
