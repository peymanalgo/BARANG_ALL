using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingShippingAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packages",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    package_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weight_kg = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    length_cm = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    width_cm = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    height_cm = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.id);
                    table.UniqueConstraint("AK_packages_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_packages_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    carrier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    shipping_method_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tracking_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    shipment_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    returned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    return_warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    last_reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_reason_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                    table.UniqueConstraint("AK_shipments_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_shipments_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_warehouses_company_id_return_warehouse_id",
                        columns: x => new { x.company_id, x.return_warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "package_items",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_package_items", x => x.id);
                    table.UniqueConstraint("AK_package_items_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_package_items_order_items_company_id_order_item_id",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_package_items_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_package_items_packages_company_id_package_id",
                        columns: x => new { x.company_id, x.package_id },
                        principalSchema: "barang",
                        principalTable: "packages",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipping_handoffs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    released_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_handoffs", x => x.id);
                    table.UniqueConstraint("AK_shipping_handoffs_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_shipping_handoffs_packages_company_id_package_id",
                        columns: x => new { x.company_id, x.package_id },
                        principalSchema: "barang",
                        principalTable: "packages",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipping_handoffs_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_packages",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_packages", x => x.id);
                    table.UniqueConstraint("AK_shipment_packages_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_shipment_packages_packages_company_id_package_id",
                        columns: x => new { x.company_id, x.package_id },
                        principalSchema: "barang",
                        principalTable: "packages",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_packages_shipments_company_id_shipment_id",
                        columns: x => new { x.company_id, x.shipment_id },
                        principalSchema: "barang",
                        principalTable: "shipments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_status_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reason_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_status_history", x => x.id);
                    table.UniqueConstraint("AK_shipment_status_history_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_shipment_status_history_shipments_company_id_shipment_id",
                        columns: x => new { x.company_id, x.shipment_id },
                        principalSchema: "barang",
                        principalTable: "shipments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000052"), "logistics.read", "logistics", "logistics.read" },
                    { new Guid("00000000-0000-0000-0000-000000000053"), "packaging.manage", "packaging", "packaging.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000054"), "shipping.manage", "shipping", "shipping.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000055"), "delivery.manage", "delivery", "delivery.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_package_items_company_id_order_id",
                schema: "barang",
                table: "package_items",
                columns: new[] { "company_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_package_items_company_id_order_item_id",
                schema: "barang",
                table: "package_items",
                columns: new[] { "company_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_package_items_company_id_package_id_order_item_id",
                schema: "barang",
                table: "package_items",
                columns: new[] { "company_id", "package_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_packages_company_id_order_id_status",
                schema: "barang",
                table: "packages",
                columns: new[] { "company_id", "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_packages_company_id_package_number",
                schema: "barang",
                table: "packages",
                columns: new[] { "company_id", "package_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_packages_company_id_package_id",
                schema: "barang",
                table: "shipment_packages",
                columns: new[] { "company_id", "package_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipment_packages_company_id_shipment_id",
                schema: "barang",
                table: "shipment_packages",
                columns: new[] { "company_id", "shipment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_status_history_company_id_shipment_id_occurred_at_~",
                schema: "barang",
                table: "shipment_status_history",
                columns: new[] { "company_id", "shipment_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_company_id_carrier_code_tracking_code",
                schema: "barang",
                table: "shipments",
                columns: new[] { "company_id", "carrier_code", "tracking_code" },
                unique: true,
                filter: "tracking_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_company_id_order_id_status",
                schema: "barang",
                table: "shipments",
                columns: new[] { "company_id", "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_company_id_return_warehouse_id",
                schema: "barang",
                table: "shipments",
                columns: new[] { "company_id", "return_warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_company_id_shipment_number",
                schema: "barang",
                table: "shipments",
                columns: new[] { "company_id", "shipment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_handoffs_company_id_package_id",
                schema: "barang",
                table: "shipping_handoffs",
                columns: new[] { "company_id", "package_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_handoffs_company_id_warehouse_id",
                schema: "barang",
                table: "shipping_handoffs",
                columns: new[] { "company_id", "warehouse_id" });
            migrationBuilder.Sql("""
                CREATE SEQUENCE IF NOT EXISTS barang.package_number_seq AS bigint START WITH 1;
                CREATE SEQUENCE IF NOT EXISTS barang.shipment_number_seq AS bigint START WITH 1;
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('logistics.read','packaging.manage','shipping.manage','delivery.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;
                ALTER TABLE barang.packages ENABLE ROW LEVEL SECURITY;CREATE POLICY packages_company_isolation ON barang.packages USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.package_items ENABLE ROW LEVEL SECURITY;CREATE POLICY package_items_company_isolation ON barang.package_items USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.shipping_handoffs ENABLE ROW LEVEL SECURITY;CREATE POLICY shipping_handoffs_company_isolation ON barang.shipping_handoffs USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.shipments ENABLE ROW LEVEL SECURITY;CREATE POLICY shipments_company_isolation ON barang.shipments USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.shipment_packages ENABLE ROW LEVEL SECURITY;CREATE POLICY shipment_packages_company_isolation ON barang.shipment_packages USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.shipment_status_history ENABLE ROW LEVEL SECURITY;CREATE POLICY shipment_status_history_company_isolation ON barang.shipment_status_history USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT USAGE,SELECT ON SEQUENCE barang.package_number_seq,barang.shipment_number_seq TO barang_app;GRANT SELECT,INSERT,UPDATE ON barang.packages,barang.shipping_handoffs,barang.shipments TO barang_app;GRANT SELECT,INSERT ON barang.package_items,barang.shipment_packages,barang.shipment_status_history TO barang_app;REVOKE DELETE ON barang.packages,barang.package_items,barang.shipping_handoffs,barang.shipments,barang.shipment_packages,barang.shipment_status_history FROM barang_app;REVOKE UPDATE ON barang.package_items,barang.shipment_packages,barang.shipment_status_history FROM barang_app;END IF;
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN GRANT USAGE,SELECT ON SEQUENCE barang.package_number_seq,barang.shipment_number_seq TO barang_test_app;GRANT SELECT,INSERT,UPDATE ON barang.packages,barang.shipping_handoffs,barang.shipments TO barang_test_app;GRANT SELECT,INSERT ON barang.package_items,barang.shipment_packages,barang.shipment_status_history TO barang_test_app;REVOKE DELETE ON barang.packages,barang.package_items,barang.shipping_handoffs,barang.shipments,barang.shipment_packages,barang.shipment_status_history FROM barang_test_app;REVOKE UPDATE ON barang.package_items,barang.shipment_packages,barang.shipment_status_history FROM barang_test_app;END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000052','00000000-0000-0000-0000-000000000053','00000000-0000-0000-0000-000000000054','00000000-0000-0000-0000-000000000055'); DROP SEQUENCE IF EXISTS barang.package_number_seq; DROP SEQUENCE IF EXISTS barang.shipment_number_seq;");
            migrationBuilder.DropTable(
                name: "package_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "shipment_packages",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "shipment_status_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "shipping_handoffs",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "shipments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "packages",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000052"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000053"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000054"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000055"));
        }
    }
}
