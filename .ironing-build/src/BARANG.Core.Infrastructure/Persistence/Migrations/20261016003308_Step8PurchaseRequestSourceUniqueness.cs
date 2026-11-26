using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Step8PurchaseRequestSourceUniqueness : Migration
    {
        private static readonly string[] SourceShortageIndexColumns = ["company_id", "source_shortage_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_purchase_requests_company_id_source_shortage_id",
                schema: "barang",
                table: "purchase_requests",
                columns: SourceShortageIndexColumns,
                unique: true,
                filter: "source_shortage_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_purchase_requests_company_id_source_shortage_id",
                schema: "barang",
                table: "purchase_requests");
        }
    }
}
