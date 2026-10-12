using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalServiceTariffDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "additional_service_charge_basis",
                schema: "barang",
                table: "tariffs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "additional_service_scope",
                schema: "barang",
                table: "tariffs",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "additional_service_charge_basis",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "additional_service_scope",
                schema: "barang",
                table: "tariffs");
        }
    }
}
