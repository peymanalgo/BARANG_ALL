using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequestNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "request_number",
                schema: "barang",
                table: "purchase_requests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE barang.purchase_requests SET request_number = 'PR-' || EXTRACT(YEAR FROM created_at_utc)::text || '-' || UPPER(SUBSTRING(REPLACE(id::text, '-', '') FROM 1 FOR 10)) WHERE request_number = '';");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requests_company_id_request_number",
                schema: "barang",
                table: "purchase_requests",
                columns: new[] { "company_id", "request_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_requests_company_id_request_number",
                schema: "barang",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "request_number",
                schema: "barang",
                table: "purchase_requests");
        }
    }
}
#pragma warning restore CA1861
