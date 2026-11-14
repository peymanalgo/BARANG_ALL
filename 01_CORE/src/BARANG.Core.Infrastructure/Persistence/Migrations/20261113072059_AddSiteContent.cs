using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

#pragma warning disable CA1861 // آرایه‌های ستون در مایگریشن فقط یک‌بار اجرا می‌شوند

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_blog_posts",
                schema: "barang",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    title_fa = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    title_en = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    excerpt_fa = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    excerpt_en = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    excerpt_ar = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    body_fa = table.Column<string>(type: "text", nullable: false),
                    body_en = table.Column<string>(type: "text", nullable: false),
                    body_ar = table.Column<string>(type: "text", nullable: false),
                    thumbnail_storage_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_blog_posts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_footer_widgets",
                schema: "barang",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    column_index = table.Column<int>(type: "integer", nullable: false),
                    widget_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title_fa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    html = table.Column<string>(type: "text", nullable: true),
                    image_storage_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_footer_widgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_main_menu_items",
                schema: "barang",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title_fa = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    title_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    title_ar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_main_menu_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_pages",
                schema: "barang",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    title_fa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body_fa = table.Column<string>(type: "text", nullable: false),
                    body_en = table.Column<string>(type: "text", nullable: false),
                    body_ar = table.Column<string>(type: "text", nullable: false),
                    placement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    footer_column = table.Column<int>(type: "integer", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "site_settings",
                schema: "barang",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_name_fa = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    site_name_en = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    site_name_ar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    logo_storage_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    favicon_storage_key = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    slogan_fa = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    slogan_en = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    slogan_ar = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_settings", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000089"), "b2c.site.read", "b2c", "b2c.site.read" },
                    { new Guid("00000000-0000-0000-0000-000000000090"), "b2c.site.manage", "b2c", "b2c.site.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_site_blog_posts_company_id_slug",
                schema: "barang",
                table: "site_blog_posts",
                columns: new[] { "company_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_footer_widgets_company_id_column_index",
                schema: "barang",
                table: "site_footer_widgets",
                columns: new[] { "company_id", "column_index" });

            migrationBuilder.CreateIndex(
                name: "IX_site_main_menu_items_company_id_display_order",
                schema: "barang",
                table: "site_main_menu_items",
                columns: new[] { "company_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_site_pages_company_id_slug",
                schema: "barang",
                table: "site_pages",
                columns: new[] { "company_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_settings_company_id",
                schema: "barang",
                table: "site_settings",
                column: "company_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_blog_posts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "site_footer_widgets",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "site_main_menu_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "site_pages",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "site_settings",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000089"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000090"));
        }
    }
}
