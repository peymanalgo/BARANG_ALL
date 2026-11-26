using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SiteContentIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "barang",
                table: "site_pages",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "barang",
                table: "site_main_menu_items",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "barang",
                table: "site_footer_widgets",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "barang",
                table: "site_blog_posts",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                schema: "barang",
                table: "site_pages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "barang",
                table: "site_main_menu_items",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "barang",
                table: "site_footer_widgets",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "barang",
                table: "site_blog_posts",
                newName: "Id");
        }
    }
}
