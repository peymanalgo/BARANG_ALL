using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Step8PurchasingApprovalPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO barang.permissions (id, code, module, technical_description)
                VALUES
                  ('00000000-0000-0000-0000-000000000086', 'purchasing.request.approve', 'purchasing', 'purchasing.request.approve'),
                  ('00000000-0000-0000-0000-000000000087', 'purchasing.order.approve', 'purchasing', 'purchasing.order.approve')
                ON CONFLICT (code) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000086"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000087"));
        }
    }
}
