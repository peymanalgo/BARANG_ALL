using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTariffsDiscountsAndTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "discount_rules",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    scope = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discount_rules", x => x.id);
                    table.CheckConstraint("ck_discount_rules_period", "valid_to IS NULL OR valid_to >= valid_from");
                    table.CheckConstraint("ck_discount_rules_value", "value >= 0 AND (discount_type <> 'Percentage' OR value <= 100)");
                });

            migrationBuilder.CreateTable(
                name: "order_price_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    raw_product_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    print_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    additional_service_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    packaging_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    taxable_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    final_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_tax_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    is_finalized = table.Column<bool>(type: "boolean", nullable: false),
                    calculated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finalized_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    calculation_version = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_price_snapshots", x => x.id);
                    table.UniqueConstraint("AK_order_price_snapshots_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_price_snapshots_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tariffs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tariff_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    print_position_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    print_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tariffs", x => x.id);
                    table.CheckConstraint("ck_tariffs_amount", "amount >= 0");
                    table.CheckConstraint("ck_tariffs_period", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "FK_tariffs_product_variants_company_id_product_variant_id",
                        columns: x => new { x.company_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_rules",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_rules", x => x.id);
                    table.CheckConstraint("ck_tax_rules_period", "valid_to IS NULL OR valid_to >= valid_from");
                    table.CheckConstraint("ck_tax_rules_rate", "rate_percent >= 0 AND rate_percent <= 100");
                });

            migrationBuilder.CreateTable(
                name: "order_item_price_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_price_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    raw_product_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    print_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    additional_service_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_allocated = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    taxable_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    final_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_price_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_item_price_snapshots_order_price_snapshots_company_id~",
                        columns: x => new { x.company_id, x.order_price_snapshot_id },
                        principalSchema: "barang",
                        principalTable: "order_price_snapshots",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_component_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_price_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_print_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tariff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    component_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tariff_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_component_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_price_component_snapshots_order_price_snapshots_company_id_~",
                        columns: x => new { x.company_id, x.order_price_snapshot_id },
                        principalSchema: "barang",
                        principalTable: "order_price_snapshots",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000030"), "pricing.read", "pricing", "pricing.read" },
                    { new Guid("00000000-0000-0000-0000-000000000031"), "pricing.manage", "pricing", "pricing.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000032"), "pricing.discount.apply", "pricing", "pricing.discount.apply" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_discount_rules_resolution",
                schema: "barang",
                table: "discount_rules",
                columns: new[] { "company_id", "code", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_price_snapshots_company_id_order_price_snapshot_~",
                schema: "barang",
                table: "order_item_price_snapshots",
                columns: new[] { "company_id", "order_price_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_price_snapshots_order_price_snapshot_id_order_it~",
                schema: "barang",
                table: "order_item_price_snapshots",
                columns: new[] { "order_price_snapshot_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_price_snapshots_company_id_order_id",
                schema: "barang",
                table: "order_price_snapshots",
                columns: new[] { "company_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "uq_order_price_snapshots_current",
                schema: "barang",
                table: "order_price_snapshots",
                column: "order_id",
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_price_component_snapshots_company_id_order_price_snapshot_id",
                schema: "barang",
                table: "price_component_snapshots",
                columns: new[] { "company_id", "order_price_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_price_component_snapshots_order_price_snapshot_id",
                schema: "barang",
                table: "price_component_snapshots",
                column: "order_price_snapshot_id");

            migrationBuilder.CreateIndex(
                name: "IX_tariffs_company_id_product_variant_id",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tariffs_resolution",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "tariff_type", "code", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_resolution",
                schema: "barang",
                table: "tax_rules",
                columns: new[] { "company_id", "code", "valid_from" });

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS btree_gist;
                ALTER TABLE barang.tariffs ADD CONSTRAINT ex_tariffs_active_period EXCLUDE USING gist
                  (company_id WITH =, tariff_type WITH =, code WITH =,
                   COALESCE(product_variant_id, '00000000-0000-0000-0000-000000000000'::uuid) WITH =,
                   COALESCE(print_position_code, '') WITH =, COALESCE(print_method_code, '') WITH =,
                   daterange(valid_from, COALESCE(valid_to, 'infinity'::date), '[]') WITH &&) WHERE (is_active);
                ALTER TABLE barang.discount_rules ADD CONSTRAINT ex_discount_rules_active_period EXCLUDE USING gist
                  (company_id WITH =, code WITH =, daterange(valid_from, COALESCE(valid_to, 'infinity'::date), '[]') WITH &&) WHERE (is_active);
                ALTER TABLE barang.tax_rules ADD CONSTRAINT ex_tax_rules_active_period EXCLUDE USING gist
                  (company_id WITH =, code WITH =, daterange(valid_from, COALESCE(valid_to, 'infinity'::date), '[]') WITH &&) WHERE (is_active);

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator' AND p.code IN ('pricing.read','pricing.manage','pricing.discount.apply')
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                ALTER TABLE barang.tariffs ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tariffs_company_isolation ON barang.tariffs USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.discount_rules ENABLE ROW LEVEL SECURITY;
                CREATE POLICY discount_rules_company_isolation ON barang.discount_rules USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.tax_rules ENABLE ROW LEVEL SECURITY;
                CREATE POLICY tax_rules_company_isolation ON barang.tax_rules USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_price_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_price_snapshots_company_isolation ON barang.order_price_snapshots USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_item_price_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_item_price_snapshots_company_isolation ON barang.order_item_price_snapshots USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.price_component_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY price_component_snapshots_company_isolation ON barang.price_component_snapshots USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.tariffs, barang.discount_rules, barang.tax_rules, barang.order_price_snapshots TO barang_app;
                    GRANT SELECT, INSERT ON barang.order_item_price_snapshots, barang.price_component_snapshots TO barang_app;
                    REVOKE DELETE ON barang.tariffs, barang.discount_rules, barang.tax_rules, barang.order_price_snapshots, barang.order_item_price_snapshots, barang.price_component_snapshots FROM barang_app;
                  END IF;
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.tariffs, barang.discount_rules, barang.tax_rules, barang.order_price_snapshots TO barang_test_app;
                    GRANT SELECT, INSERT ON barang.order_item_price_snapshots, barang.price_component_snapshots TO barang_test_app;
                    REVOKE DELETE ON barang.tariffs, barang.discount_rules, barang.tax_rules, barang.order_price_snapshots, barang.order_item_price_snapshots, barang.price_component_snapshots FROM barang_test_app;
                  END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000030','00000000-0000-0000-0000-000000000031','00000000-0000-0000-0000-000000000032');");
            migrationBuilder.DropTable(
                name: "discount_rules",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_item_price_snapshots",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "price_component_snapshots",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "tariffs",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "tax_rules",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_price_snapshots",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000032"));
        }
    }
}
