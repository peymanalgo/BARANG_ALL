using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupplierSourcingPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "availability",
                schema: "barang",
                table: "supplier_products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Available");

            migrationBuilder.AddColumn<string>(
                name: "brand",
                schema: "barang",
                table: "supplier_products",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "barang",
                table: "supplier_products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_order_amount",
                schema: "barang",
                table: "supplier_products",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "supplier_products",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "packaging_type_id",
                schema: "barang",
                table: "supplier_products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "purchase_unit_id",
                schema: "barang",
                table: "supplier_products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supplier_item_name",
                schema: "barang",
                table: "supplier_products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_supplier_products_company_supplier_id",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "supplier_id", "id" });

            migrationBuilder.CreateTable(
                name: "supplier_price_lists",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_price_lists", x => x.id);
                    table.UniqueConstraint("ak_supplier_price_lists_company_id_id", x => new { x.company_id, x.id });
                    table.UniqueConstraint("ak_supplier_price_lists_company_supplier_id", x => new { x.company_id, x.supplier_id, x.id });
                    table.CheckConstraint("ck_supplier_price_lists_period", "currency_code ~ '^[A-Z]{3}$' AND (effective_to IS NULL OR effective_to >= effective_from) AND (NOT is_active OR published_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_supplier_price_lists_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_price_lists_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_lists_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_product_prices",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    purchase_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    minimum_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    freight_per_unit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_product_prices", x => x.id);
                    table.UniqueConstraint("ak_supplier_prices_company_supplier_product_id", x => new { x.company_id, x.supplier_id, x.supplier_product_id, x.id });
                    table.UniqueConstraint("ak_supplier_product_prices_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_prices_period", "currency_code ~ '^[A-Z]{3}$' AND (effective_to IS NULL OR effective_to >= effective_from) AND (NOT is_active OR published_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_supplier_prices_terms", "unit_price >= 0 AND minimum_quantity >= 0 AND discount_percent BETWEEN 0 AND 100 AND tax_rate_percent BETWEEN 0 AND 100 AND freight_per_unit >= 0");
                    table.CheckConstraint("ck_supplier_product_prices_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_lookup_values_purchase_unit_id",
                        column: x => x.purchase_unit_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_lookup_values_tax_status_id",
                        column: x => x.tax_status_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_supplier_price_lists_company_id_sup~",
                        columns: x => new { x.company_id, x.supplier_id, x.price_list_id },
                        principalSchema: "barang",
                        principalTable: "supplier_price_lists",
                        principalColumns: new[] { "company_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_supplier_products_company_id_suppli~",
                        columns: x => new { x.company_id, x.supplier_id, x.supplier_product_id },
                        principalSchema: "barang",
                        principalTable: "supplier_products",
                        principalColumns: new[] { "company_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_product_prices_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_price_list_lines",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_price_id = table.Column<Guid>(type: "uuid", nullable: true),
                    purchase_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    minimum_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    tax_status_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tax_rate_percent = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    freight_per_unit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_price_list_lines", x => x.id);
                    table.UniqueConstraint("ak_supplier_price_list_lines_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_price_list_lines_terms", "unit_price >= 0 AND minimum_quantity >= 0 AND discount_percent BETWEEN 0 AND 100 AND tax_rate_percent BETWEEN 0 AND 100 AND freight_per_unit >= 0");
                    table.CheckConstraint("ck_supplier_price_list_lines_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_lookup_values_purchase_unit_id",
                        column: x => x.purchase_unit_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_lookup_values_tax_status_id",
                        column: x => x.tax_status_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_supplier_price_lists_company_id_s~",
                        columns: x => new { x.company_id, x.supplier_id, x.price_list_id },
                        principalSchema: "barang",
                        principalTable: "supplier_price_lists",
                        principalColumns: new[] { "company_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_supplier_product_prices_company_i~",
                        columns: x => new { x.company_id, x.supplier_id, x.supplier_product_id, x.published_price_id },
                        principalSchema: "barang",
                        principalTable: "supplier_product_prices",
                        principalColumns: new[] { "company_id", "supplier_id", "supplier_product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_supplier_products_company_id_supp~",
                        columns: x => new { x.company_id, x.supplier_id, x.supplier_product_id },
                        principalSchema: "barang",
                        principalTable: "supplier_products",
                        principalColumns: new[] { "company_id", "supplier_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_price_list_lines_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_products_packaging_type_id",
                schema: "barang",
                table: "supplier_products",
                column: "packaging_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_products_purchase_unit_id",
                schema: "barang",
                table: "supplier_products",
                column: "purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_products_preferred_product",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "product_id" },
                unique: true,
                filter: "product_variant_id IS NULL AND is_preferred AND is_active");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_products_preferred_variant",
                schema: "barang",
                table: "supplier_products",
                columns: new[] { "company_id", "product_variant_id" },
                unique: true,
                filter: "product_variant_id IS NOT NULL AND is_preferred AND is_active");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_products_availability",
                schema: "barang",
                table: "supplier_products",
                sql: "availability IN ('Available','OnRequest','Unavailable')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_supplier_products_sourcing_amount",
                schema: "barang",
                table: "supplier_products",
                sql: "minimum_order_amount IS NULL OR minimum_order_amount >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_price_list_lines_company_id_supplier_id_price_list~",
                schema: "barang",
                table: "supplier_price_list_lines",
                columns: new[] { "company_id", "supplier_id", "price_list_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_price_list_lines_company_id_supplier_id_supplier_p~",
                schema: "barang",
                table: "supplier_price_list_lines",
                columns: new[] { "company_id", "supplier_id", "supplier_product_id", "published_price_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_price_list_lines_purchase_unit_id",
                schema: "barang",
                table: "supplier_price_list_lines",
                column: "purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_price_list_lines_tax_status_id",
                schema: "barang",
                table: "supplier_price_list_lines",
                column: "tax_status_id");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_price_list_lines_break",
                schema: "barang",
                table: "supplier_price_list_lines",
                columns: new[] { "company_id", "price_list_id", "supplier_product_id", "purchase_unit_id", "minimum_quantity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_price_lists_directory",
                schema: "barang",
                table: "supplier_price_lists",
                columns: new[] { "company_id", "supplier_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "uq_supplier_price_lists_code",
                schema: "barang",
                table: "supplier_price_lists",
                columns: new[] { "company_id", "supplier_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_supplier_prices_history",
                schema: "barang",
                table: "supplier_product_prices",
                columns: new[] { "company_id", "supplier_id", "supplier_product_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_prices_resolution",
                schema: "barang",
                table: "supplier_product_prices",
                columns: new[] { "company_id", "supplier_product_id", "purchase_unit_id", "currency_code", "minimum_quantity" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_prices_company_id_supplier_id_price_list_id",
                schema: "barang",
                table: "supplier_product_prices",
                columns: new[] { "company_id", "supplier_id", "price_list_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_prices_purchase_unit_id",
                schema: "barang",
                table: "supplier_product_prices",
                column: "purchase_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_product_prices_tax_status_id",
                schema: "barang",
                table: "supplier_product_prices",
                column: "tax_status_id");

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_products_lookup_values_packaging_type_id",
                schema: "barang",
                table: "supplier_products",
                column: "packaging_type_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_products_lookup_values_purchase_unit_id",
                schema: "barang",
                table: "supplier_products",
                column: "purchase_unit_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(SupplierSourcingMigrationSql.AfterSchema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SupplierSourcingMigrationSql.BeforeRollback);

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_products_lookup_values_packaging_type_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropForeignKey(
                name: "FK_supplier_products_lookup_values_purchase_unit_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropTable(
                name: "supplier_price_list_lines",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_product_prices",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_price_lists",
                schema: "barang");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_supplier_products_company_supplier_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropIndex(
                name: "IX_supplier_products_packaging_type_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropIndex(
                name: "IX_supplier_products_purchase_unit_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropIndex(
                name: "uq_supplier_products_preferred_product",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropIndex(
                name: "uq_supplier_products_preferred_variant",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_products_availability",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropCheckConstraint(
                name: "ck_supplier_products_sourcing_amount",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "availability",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "brand",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "minimum_order_amount",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "packaging_type_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "purchase_unit_id",
                schema: "barang",
                table: "supplier_products");

            migrationBuilder.DropColumn(
                name: "supplier_item_name",
                schema: "barang",
                table: "supplier_products");
        }
    }
}
