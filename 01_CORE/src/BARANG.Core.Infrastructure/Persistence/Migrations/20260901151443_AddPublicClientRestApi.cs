using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF migration-generated constant arrays

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicClientRestApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000063"), "catalog.read", "catalog", "catalog.read" },
                    { new Guid("00000000-0000-0000-0000-000000000064"), "customer.profile.read", "customer", "customer.profile.read" },
                    { new Guid("00000000-0000-0000-0000-000000000065"), "customer.profile.write", "customer", "customer.profile.write" },
                    { new Guid("00000000-0000-0000-0000-000000000066"), "client.order.read", "client", "client.order.read" },
                    { new Guid("00000000-0000-0000-0000-000000000067"), "client.order.write", "client", "client.order.write" },
                    { new Guid("00000000-0000-0000-0000-000000000068"), "client.file.upload", "client", "client.file.upload" },
                    { new Guid("00000000-0000-0000-0000-000000000069"), "pricing.preview", "pricing", "pricing.preview" },
                    { new Guid("00000000-0000-0000-0000-000000000070"), "tracking.read", "tracking", "tracking.read" }
                });
            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.id IN ('00000000-0000-0000-0000-000000000063','00000000-0000-0000-0000-000000000064','00000000-0000-0000-0000-000000000065','00000000-0000-0000-0000-000000000066','00000000-0000-0000-0000-000000000067','00000000-0000-0000-0000-000000000068','00000000-0000-0000-0000-000000000069','00000000-0000-0000-0000-000000000070') ON CONFLICT(role_id,permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000063','00000000-0000-0000-0000-000000000064','00000000-0000-0000-0000-000000000065','00000000-0000-0000-0000-000000000066','00000000-0000-0000-0000-000000000067','00000000-0000-0000-0000-000000000068','00000000-0000-0000-0000-000000000069','00000000-0000-0000-0000-000000000070');");
            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000063"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000064"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000065"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000066"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000067"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000068"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000069"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000070"));
        }
    }
}
