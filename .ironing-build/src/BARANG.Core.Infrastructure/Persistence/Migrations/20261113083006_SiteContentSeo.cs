using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SiteContentSeo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cover_storage_key",
                schema: "barang",
                table: "site_pages",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_ar",
                schema: "barang",
                table: "site_pages",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_en",
                schema: "barang",
                table: "site_pages",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_fa",
                schema: "barang",
                table: "site_pages",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_ar",
                schema: "barang",
                table: "site_pages",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_en",
                schema: "barang",
                table: "site_pages",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_fa",
                schema: "barang",
                table: "site_pages",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_ar",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_en",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description_fa",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_ar",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_en",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_keywords_fa",
                schema: "barang",
                table: "site_blog_posts",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_storage_key",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_description_ar",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_description_en",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_description_fa",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_keywords_ar",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_keywords_en",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_keywords_fa",
                schema: "barang",
                table: "site_pages");

            migrationBuilder.DropColumn(
                name: "seo_description_ar",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_description_en",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_description_fa",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_keywords_ar",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_keywords_en",
                schema: "barang",
                table: "site_blog_posts");

            migrationBuilder.DropColumn(
                name: "seo_keywords_fa",
                schema: "barang",
                table: "site_blog_posts");
        }
    }
}
