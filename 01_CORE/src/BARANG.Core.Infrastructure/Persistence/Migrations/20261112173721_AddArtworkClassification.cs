using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "audience_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "license_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "style_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "subcategory_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "theme_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "type_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "usage_id",
                schema: "barang",
                table: "artworks",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audience_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "license_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "style_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "subcategory_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "theme_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "type_id",
                schema: "barang",
                table: "artworks");

            migrationBuilder.DropColumn(
                name: "usage_id",
                schema: "barang",
                table: "artworks");
        }
    }
}
