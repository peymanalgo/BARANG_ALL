using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_company_id_reference",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "purchase_receipts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "receipt_date_utc",
                schema: "barang",
                table: "purchase_receipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "receipt_number",
                schema: "barang",
                table: "purchase_receipts",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "barang",
                table: "purchase_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE barang.purchase_receipts SET receipt_number = reference, status = 1, receipt_date_utc = received_at_utc WHERE receipt_number = '';");
            migrationBuilder.Sql("GRANT UPDATE, DELETE ON barang.purchase_receipts TO barang_app;");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_company_id_receipt_number",
                schema: "barang",
                table: "purchase_receipts",
                columns: new[] { "company_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_company_id_reference",
                schema: "barang",
                table: "purchase_receipts",
                columns: new[] { "company_id", "reference" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_company_id_receipt_number",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_company_id_reference",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropColumn(
                name: "receipt_date_utc",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropColumn(
                name: "receipt_number",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "barang",
                table: "purchase_receipts");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_company_id_reference",
                schema: "barang",
                table: "purchase_receipts",
                columns: new[] { "company_id", "reference" },
                unique: true);
        }
    }
}
#pragma warning restore CA1861
