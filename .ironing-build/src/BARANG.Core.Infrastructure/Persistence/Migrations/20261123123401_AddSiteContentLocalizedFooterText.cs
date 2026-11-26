using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteContentLocalizedFooterText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "text_ar",
                schema: "barang",
                table: "site_footer_widgets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_en",
                schema: "barang",
                table: "site_footer_widgets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_es",
                schema: "barang",
                table: "site_footer_widgets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text_fa",
                schema: "barang",
                table: "site_footer_widgets",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "text_ar",
                schema: "barang",
                table: "site_footer_widgets");

            migrationBuilder.DropColumn(
                name: "text_en",
                schema: "barang",
                table: "site_footer_widgets");

            migrationBuilder.DropColumn(
                name: "text_es",
                schema: "barang",
                table: "site_footer_widgets");

            migrationBuilder.DropColumn(
                name: "text_fa",
                schema: "barang",
                table: "site_footer_widgets");
        }
    }
}
