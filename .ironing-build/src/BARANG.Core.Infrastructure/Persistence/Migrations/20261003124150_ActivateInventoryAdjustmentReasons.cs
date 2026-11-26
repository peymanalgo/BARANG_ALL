using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActivateInventoryAdjustmentReasons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE barang.lookup_values
                SET is_active = TRUE, updated_at_utc = CURRENT_TIMESTAMP, version = version + 1
                WHERE lookup_type_id IN (SELECT id FROM barang.lookup_types WHERE code = 'inventory.adjustment.reason');");
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ux_stock_transactions_opening_balance
                ON barang.stock_transactions (company_id, warehouse_id, product_variant_id)
                WHERE transaction_type = 'OpeningBalance';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS barang.ux_stock_transactions_opening_balance;");
        }
    }
}
