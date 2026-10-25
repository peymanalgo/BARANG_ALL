using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkCategoryId : Migration
    {
        private static readonly string[] ArtworkCategoryIndexColumns = ["company_id", "category_id"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE barang.artworks AS a SET category_id = v.id FROM barang.lookup_values AS v JOIN barang.lookup_types AS t ON t.id = v.lookup_type_id WHERE a.category_id IS NULL AND t.code = 'artwork.category' AND upper(v.code) = upper(a.category_code)");

            migrationBuilder.CreateIndex(
                name: "ix_artworks_category_id",
                schema: "barang",
                table: "artworks",
                columns: ArtworkCategoryIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_artworks_category_id",
                schema: "barang",
                table: "artworks",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_artworks_lookup_values_category_id",
                schema: "barang",
                table: "artworks",
                column: "category_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_artworks_lookup_values_category_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropIndex(
                name: "ix_artworks_category_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropIndex(
                name: "IX_artworks_category_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "barang",
                table: "artworks");
        }
    }
}
