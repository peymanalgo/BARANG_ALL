using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkCategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_id",
                schema: "barang",
                table: "lookup_values",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_values_parent_id",
                schema: "barang",
                table: "lookup_values",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "fk_lookup_values_parent_id",
                schema: "barang",
                table: "lookup_values",
                column: "parent_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_lookup_values_parent_id",
                schema: "barang",
                table: "lookup_values");

            migrationBuilder.DropIndex(
                name: "IX_lookup_values_parent_id",
                schema: "barang",
                table: "lookup_values");

            migrationBuilder.DropColumn(
                name: "parent_id",
                schema: "barang",
                table: "lookup_values");
        }
    }
}
