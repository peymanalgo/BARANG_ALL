using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSupplierEquipmentFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    equipment_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    operational_status_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    model = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    serial_number = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment", x => x.id);
                    table.UniqueConstraint("ak_equipment_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_branches_company_id_branch_id",
                        columns: x => new { x.company_id, x.branch_id },
                        principalSchema: "barang",
                        principalTable: "branches",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_organizational_units_company_id_organizational_un~",
                        columns: x => new { x.company_id, x.organizational_unit_id },
                        principalSchema: "barang",
                        principalTable: "organizational_units",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    product_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_of_measure_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.UniqueConstraint("ak_products_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_products_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_products_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    registration_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    primary_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    primary_phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    primary_mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.UniqueConstraint("ak_suppliers_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_suppliers_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_suppliers_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    size_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.UniqueConstraint("ak_product_variants_company_id_id", x => new { x.company_id, x.id });
                    table.UniqueConstraint("ak_product_variants_company_product_id", x => new { x.company_id, x.product_id, x.id });
                    table.CheckConstraint("ck_product_variants_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_product_variants_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_variants_products_company_id_product_id",
                        columns: x => new { x.company_id, x.product_id },
                        principalSchema: "barang",
                        principalTable: "products",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_products",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_item_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_order_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    lead_time_days = table.Column<int>(type: "integer", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_products", x => x.id);
                    table.UniqueConstraint("ak_supplier_products_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_products_lead_nonnegative", "lead_time_days IS NULL OR lead_time_days >= 0");
                    table.CheckConstraint("ck_supplier_products_moq_nonnegative", "minimum_order_quantity IS NULL OR minimum_order_quantity >= 0");
                    table.CheckConstraint("ck_supplier_products_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_products_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_products_product_variants_company_id_product_id_pr~",
                        columns: x => new { x.company_id, x.product_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_products_products_company_id_product_id",
                        columns: x => new { x.company_id, x.product_id },
                        principalSchema: "barang",
                        principalTable: "products",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_products_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000013"), "product.read", "product", "product.read" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), "product.manage", "product", "product.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000015"), "supplier.read", "supplier", "supplier.read" },
                    { new Guid("00000000-0000-0000-0000-000000000016"), "supplier.manage", "supplier", "supplier.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000017"), "equipment.read", "equipment", "equipment.read" },
                    { new Guid("00000000-0000-0000-0000-000000000018"), "equipment.manage", "equipment", "equipment.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_branch",
                schema: "barang",
                table: "equipment",
                columns: new[] { "company_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_company_id_organizational_unit_id",
                schema: "barang",
                table: "equipment",
                columns: new[] { "company_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_directory",
                schema: "barang",
                table: "equipment",
                columns: new[] { "company_id", "is_active", "equipment_type_code", "operational_status_code" });

            migrationBuilder.CreateIndex(
                name: "uq_equipment_company_code",
                schema: "barang",
                table: "equipment",
                columns: new[] { "company_id", "equipment_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_directory",
                schema: "barang",
                table: "product_variants",
                columns: new[] { "company_id", "product_id", "is_active", "sku" });

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_company_barcode",
                schema: "barang",
                table: "product_variants",
                columns: new[] { "company_id", "barcode" },
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_product_variants_company_sku",
                schema: "barang",
                table: "product_variants",
                columns: new[] { "company_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_company_name",
                schema: "barang",
                table: "products",
                columns: new[] { "company_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_products_directory",
                schema: "barang",
                table: "products",
                columns: new[] { "company_id", "is_active", "product_code" });

            migrationBuilder.CreateIndex(
                name: "uq_products_company_code",
                schema: "barang",
                table: "products",
                columns: new[] { "company_id", "product_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_products_company_id_product_id_product_variant_id",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_products_product",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "product_id", "is_preferred", "is_active" });

            migrationBuilder.CreateIndex(
                name: "uq_supplier_products_product_level",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "supplier_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_products_variant_level",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "supplier_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_company_name",
                schema: "barang",
                table: "suppliers",
                columns: new[] { "company_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_directory",
                schema: "barang",
                table: "suppliers",
                columns: new[] { "company_id", "is_active", "supplier_code" });

            migrationBuilder.CreateIndex(
                name: "uq_suppliers_company_code",
                schema: "barang",
                table: "suppliers",
                columns: new[] { "company_id", "supplier_code" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.code IN ('product.read', 'product.manage', 'supplier.read', 'supplier.manage', 'equipment.read', 'equipment.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                ALTER TABLE barang.products ENABLE ROW LEVEL SECURITY;
                CREATE POLICY products_company_isolation ON barang.products
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.product_variants ENABLE ROW LEVEL SECURITY;
                CREATE POLICY product_variants_company_isolation ON barang.product_variants
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.suppliers ENABLE ROW LEVEL SECURITY;
                CREATE POLICY suppliers_company_isolation ON barang.suppliers
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.supplier_products ENABLE ROW LEVEL SECURITY;
                CREATE POLICY supplier_products_company_isolation ON barang.supplier_products
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.equipment ENABLE ROW LEVEL SECURITY;
                CREATE POLICY equipment_company_isolation ON barang.equipment
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.products, barang.product_variants, barang.suppliers, barang.supplier_products, barang.equipment TO barang_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000013', '00000000-0000-0000-0000-000000000014', '00000000-0000-0000-0000-000000000015', '00000000-0000-0000-0000-000000000016', '00000000-0000-0000-0000-000000000017', '00000000-0000-0000-0000-000000000018');");
            migrationBuilder.DropTable(
                name: "equipment",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_products",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "products",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000018"));
        }
    }
}
