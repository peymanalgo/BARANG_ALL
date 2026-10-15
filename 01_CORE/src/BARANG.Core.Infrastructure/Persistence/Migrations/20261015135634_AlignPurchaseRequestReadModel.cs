using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPurchaseRequestReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE barang.purchase_requests ADD COLUMN IF NOT EXISTS request_number character varying(40) NOT NULL DEFAULT ''; UPDATE barang.purchase_requests SET request_number = 'PR-' || EXTRACT(YEAR FROM created_at_utc)::text || '-' || UPPER(SUBSTRING(REPLACE(id::text, '-', '') FROM 1 FOR 10)) WHERE request_number = ''; CREATE UNIQUE INDEX IF NOT EXISTS \"IX_purchase_requests_company_id_request_number\" ON barang.purchase_requests (company_id, request_number);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "request_number",
                schema: "barang",
                table: "purchase_requests");
        }
    }
}
