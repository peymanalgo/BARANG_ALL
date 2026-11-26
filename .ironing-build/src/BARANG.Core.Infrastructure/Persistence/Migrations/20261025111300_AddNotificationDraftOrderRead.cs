using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDraftOrderRead : Migration
    {
        private static readonly string[] PermissionColumns = ["id", "code", "module", "technical_description"];
        private static readonly object[] PermissionValues = [new Guid("00000000-0000-0000-0000-000000000088"), "notification.draftorder.read", "notification", "notification.draftorder.read"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: PermissionColumns,
                values: PermissionValues);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000088"));
        }
    }
}
