using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CartableV3BusinessPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "priority",
                schema: "barang",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.CreateIndex(
                name: "ix_orders_priority_created",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "priority", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_priority_created",
                schema: "barang",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "priority",
                schema: "barang",
                table: "orders");
        }
    }
}
