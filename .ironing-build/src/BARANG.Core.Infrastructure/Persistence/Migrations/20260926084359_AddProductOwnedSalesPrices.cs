using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOwnedSalesPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_sales_prices",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    migrated_from_tariff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_sales_prices", x => x.id);
                    table.UniqueConstraint("ak_product_sales_prices_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_product_sales_prices_amount", "amount >= 0");
                    table.CheckConstraint("ck_product_sales_prices_period", "end_date IS NULL OR end_date >= start_date");
                    table.CheckConstraint("ck_product_sales_prices_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_product_sales_prices_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_sales_prices_product_variants_company_id_product_id~",
                        columns: x => new { x.company_id, x.product_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_sales_prices_products_company_id_product_id",
                        columns: x => new { x.company_id, x.product_id },
                        principalSchema: "barang",
                        principalTable: "products",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_sales_prices_resolution",
                schema: "barang",
                table: "product_sales_prices",
                columns: new[] { "company_id", "product_id", "product_variant_id", "start_date" });

            migrationBuilder.Sql(@"
                INSERT INTO barang.product_sales_prices
                    (id, product_id, product_variant_id, amount, currency_code, start_date, end_date,
                     migrated_from_tariff_id, company_id, is_active, created_at_utc, updated_at_utc, version)
                SELECT t.id, t.product_id, t.product_variant_id, t.amount, t.currency_code,
                       t.valid_from, t.valid_to, t.id, t.company_id, t.is_active,
                       t.created_at_utc, t.updated_at_utc, t.version
                FROM barang.tariffs t
                WHERE t.tariff_type = 'RawProduct' AND t.product_id IS NOT NULL
                ON CONFLICT (id) DO NOTHING;

                ALTER TABLE barang.product_sales_prices ENABLE ROW LEVEL SECURITY;
                CREATE POLICY product_sales_prices_company_isolation ON barang.product_sales_prices
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                GRANT SELECT, INSERT, UPDATE ON barang.product_sales_prices TO barang_app;
                GRANT SELECT, INSERT, UPDATE ON barang.product_sales_prices TO barang_test_app;
                REVOKE DELETE ON barang.product_sales_prices FROM barang_app;
                REVOKE DELETE ON barang.product_sales_prices FROM barang_test_app;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_sales_prices",
                schema: "barang");
        }
    }
}
