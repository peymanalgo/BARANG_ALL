using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRawProductTariffTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tariffs_resolution",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                schema: "barang",
                table: "tariffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tariffs_company_id_product_id",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "product_id" });

            migrationBuilder.CreateIndex(
                name: "ix_tariffs_resolution",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "tariff_type", "code", "product_id", "product_variant_id", "valid_from" });

            migrationBuilder.AddForeignKey(
                name: "FK_tariffs_products_company_id_product_id",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "product_id" },
                principalSchema: "barang",
                principalTable: "products",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tariffs_products_company_id_product_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropIndex(
                name: "IX_tariffs_company_id_product_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropIndex(
                name: "ix_tariffs_resolution",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "product_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.CreateIndex(
                name: "ix_tariffs_resolution",
                schema: "barang",
                table: "tariffs",
                columns: new[] { "company_id", "tariff_type", "code", "valid_from" });
        }
    }
}
#pragma warning restore CA1861
