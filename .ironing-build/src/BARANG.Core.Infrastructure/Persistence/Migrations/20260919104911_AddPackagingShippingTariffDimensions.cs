using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPackagingShippingTariffDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "logistics_charge_basis",
                schema: "barang",
                table: "tariffs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "packaging_type_code",
                schema: "barang",
                table: "tariffs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipping_method_code",
                schema: "barang",
                table: "tariffs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "logistics_charge_basis",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "packaging_type_code",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "shipping_method_code",
                schema: "barang",
                table: "tariffs");
        }
    }
}
