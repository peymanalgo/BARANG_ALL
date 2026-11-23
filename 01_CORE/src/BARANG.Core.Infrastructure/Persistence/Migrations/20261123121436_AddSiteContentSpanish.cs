using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteContentSpanish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "site_name_es",
                schema: "barang",
                table: "site_settings",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "slogan_es",
                schema: "barang",
                table: "site_settings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_es",
                schema: "barang",
                table: "site_pages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "seo_description_es",
                schema: "barang",
                table: "site_pages",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_es",
                schema: "barang",
                table: "site_pages",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_es",
                schema: "barang",
                table: "site_pages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title_es",
                schema: "barang",
                table: "site_main_menu_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_es",
                schema: "barang",
                table: "site_footer_widgets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "body_es",
                schema: "barang",
                table: "site_blog_posts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "excerpt_es",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "seo_description_es",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_es",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title_es",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "site_name_es",
                schema: "barang",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "slogan_es",
                schema: "barang",
                table: "site_settings");

            migrationBuilder.DropColumn(
                name: "body_es",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_description_es",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_keywords_es",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "title_es",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "title_es",
                schema: "barang",
                table: "site_main_menu_items");

            migrationBuilder.DropColumn(
                name: "title_es",
                schema: "barang",
                table: "site_footer_widgets");

            migrationBuilder.DropColumn(
                name: "body_es",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "excerpt_es",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_description_es",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_keywords_es",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "title_es",
                schema: "barang",
                table: "site_blog_posts");
        }
    }
}
