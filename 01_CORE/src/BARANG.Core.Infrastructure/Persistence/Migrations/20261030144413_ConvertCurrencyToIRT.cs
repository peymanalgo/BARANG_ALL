using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCurrencyToIRT : Migration
    {
        private static string UpdateSql(string table) =>
            "DO $cur$ DECLARE cid uuid; BEGIN " +
            "FOR cid IN SELECT id FROM barang.companies LOOP " +
            "PERFORM set_config('barang.company_id', cid::text, false); " +
            "UPDATE barang." + table + " SET currency_code='IRT' WHERE currency_code='IRR' AND company_id=cid; " +
            "END LOOP; END $cur$;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UpdateSql("discount_rules"));
            migrationBuilder.Sql(UpdateSql("product_sales_prices"));
            migrationBuilder.Sql(UpdateSql("order_price_snapshots"));
            migrationBuilder.Sql(UpdateSql("tax_rules"));
            migrationBuilder.Sql(UpdateSql("tariffs"));
            migrationBuilder.Sql(UpdateSql("purchase_orders"));
            migrationBuilder.Sql(UpdateSql("payments"));
            migrationBuilder.Sql(UpdateSql("invoice_snapshots"));
            migrationBuilder.Sql(UpdateSql("customer_bank_accounts"));
            migrationBuilder.Sql(UpdateSql("supplier_bank_accounts"));
            migrationBuilder.Sql(UpdateSql("supplier_product_prices"));
            migrationBuilder.Sql(UpdateSql("supplier_price_lists"));
            migrationBuilder.Sql(UpdateSql("supplier_contracts"));
            migrationBuilder.Sql(UpdateSql("equipment"));
            migrationBuilder.Sql(UpdateSql("customer_wallet_transactions"));
            migrationBuilder.Sql(UpdateSql("customer_wallet_topups"));
            migrationBuilder.Sql(UpdateSql("customer_referrals"));
            migrationBuilder.Sql(
                "UPDATE barang.companies SET default_currency_code='IRT' WHERE default_currency_code='IRR';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
