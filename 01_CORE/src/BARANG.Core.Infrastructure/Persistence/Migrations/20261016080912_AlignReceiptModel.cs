using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignReceiptModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE barang.purchase_receipts ADD COLUMN IF NOT EXISTS receipt_number character varying(40) NOT NULL DEFAULT ''; CREATE UNIQUE INDEX IF NOT EXISTS \"IX_purchase_receipts_company_id_receipt_number\" ON barang.purchase_receipts (company_id, receipt_number);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_company_id_receipt_number",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropColumn(
                name: "receipt_number",
                schema: "barang",
                table: "purchase_receipts");
        }
    }
}
