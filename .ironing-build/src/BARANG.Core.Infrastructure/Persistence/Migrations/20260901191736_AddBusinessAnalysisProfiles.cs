using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAnalysisProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_products_id_company_id",
                schema: "barang",
                table: "products",
                columns: new[] { "id", "company_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_product_variants_id_company_id",
                schema: "barang",
                table: "product_variants",
                columns: new[] { "id", "company_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_orders_id_company_id",
                schema: "barang",
                table: "orders",
                columns: new[] { "id", "company_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_order_print_operations_id_company_id",
                schema: "barang",
                table: "order_print_operations",
                columns: new[] { "id", "company_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_equipment_id_company_id",
                schema: "barang",
                table: "equipment",
                columns: new[] { "id", "company_id" });

            migrationBuilder.CreateTable(
                name: "customer_business_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    economic_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    business_details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_business_profiles", x => x.id);
                    table.UniqueConstraint("AK_customer_business_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_customer_business_profiles_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_business_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipping_method_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    packaging_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    include_invoice_in_package = table.Column<bool>(type: "boolean", nullable: false),
                    order_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_business_profiles", x => x.id);
                    table.UniqueConstraint("AK_order_business_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_business_profiles_orders_order_id_company_id",
                        columns: x => new { x.order_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "print_technical_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_print_operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    print_size_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    print_count = table.Column<int>(type: "integer", nullable: false),
                    printer_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    heat_press_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    specifications = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_technical_profiles", x => x.id);
                    table.UniqueConstraint("AK_print_technical_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_print_technical_profiles_equipment_heat_press_equipment_id_~",
                        columns: x => new { x.heat_press_equipment_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_technical_profiles_equipment_printer_equipment_id_com~",
                        columns: x => new { x.printer_equipment_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_print_technical_profiles_order_print_operations_order_print~",
                        columns: x => new { x.order_print_operation_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "order_print_operations",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_technical_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    brand_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    material_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gender_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fit_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    style_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    technical_attributes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_technical_profiles", x => x.id);
                    table.UniqueConstraint("AK_product_technical_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_product_technical_profiles_product_variants_product_variant~",
                        columns: x => new { x.product_variant_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_technical_profiles_products_product_id_company_id",
                        columns: x => new { x.product_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "products",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_business_profiles_company_id_customer_id",
                schema: "barang",
                table: "customer_business_profiles",
                columns: new[] { "company_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_business_profiles_customer_id_company_id",
                schema: "barang",
                table: "customer_business_profiles",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_business_profiles_company_id_order_id",
                schema: "barang",
                table: "order_business_profiles",
                columns: new[] { "company_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_business_profiles_order_id_company_id",
                schema: "barang",
                table: "order_business_profiles",
                columns: new[] { "order_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_print_technical_profiles_company_id_order_print_operation_id",
                schema: "barang",
                table: "print_technical_profiles",
                columns: new[] { "company_id", "order_print_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_technical_profiles_heat_press_equipment_id_company_id",
                schema: "barang",
                table: "print_technical_profiles",
                columns: new[] { "heat_press_equipment_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_print_technical_profiles_order_print_operation_id_company_id",
                schema: "barang",
                table: "print_technical_profiles",
                columns: new[] { "order_print_operation_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_print_technical_profiles_printer_equipment_id_company_id",
                schema: "barang",
                table: "print_technical_profiles",
                columns: new[] { "printer_equipment_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_technical_profiles_company_id_product_id_product_va~",
                schema: "barang",
                table: "product_technical_profiles",
                columns: new[] { "company_id", "product_id", "product_variant_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_product_technical_profiles_product_id_company_id",
                schema: "barang",
                table: "product_technical_profiles",
                columns: new[] { "product_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_technical_profiles_product_variant_id_company_id",
                schema: "barang",
                table: "product_technical_profiles",
                columns: new[] { "product_variant_id", "company_id" });

            migrationBuilder.Sql("""
                ALTER TABLE barang.customer_business_profiles ADD CONSTRAINT ck_customer_business_profile_credit CHECK (credit_limit >= 0 AND payment_terms_days BETWEEN 0 AND 3650);
                ALTER TABLE barang.print_technical_profiles ADD CONSTRAINT ck_print_technical_profile_count CHECK (print_count > 0);
                DO $profiles$
                DECLARE t text;
                BEGIN
                  FOREACH t IN ARRAY ARRAY['customer_business_profiles','product_technical_profiles','order_business_profiles','print_technical_profiles'] LOOP
                    EXECUTE format('ALTER TABLE barang.%I ENABLE ROW LEVEL SECURITY',t);
                    EXECUTE format('CREATE POLICY %I ON barang.%I USING(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid) WITH CHECK(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid)',t||'_company_isolation',t);
                    IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
                      EXECUTE format('GRANT SELECT,INSERT,UPDATE ON barang.%I TO barang_app',t);
                      EXECUTE format('REVOKE DELETE ON barang.%I FROM barang_app',t);
                    END IF;
                  END LOOP;
                END $profiles$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_business_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_business_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "print_technical_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "product_technical_profiles",
                schema: "barang");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_products_id_company_id",
                schema: "barang",
                table: "products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_product_variants_id_company_id",
                schema: "barang",
                table: "product_variants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_orders_id_company_id",
                schema: "barang",
                table: "orders");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_order_print_operations_id_company_id",
                schema: "barang",
                table: "order_print_operations");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_equipment_id_company_id",
                schema: "barang",
                table: "equipment");
        }
    }
}
