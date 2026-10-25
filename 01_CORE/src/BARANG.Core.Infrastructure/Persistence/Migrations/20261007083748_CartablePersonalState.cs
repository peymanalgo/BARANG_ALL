using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CartablePersonalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cartable_personal_states",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    runtime_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_starred = table.Column<bool>(type: "boolean", nullable: false),
                    seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartable_personal_states", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cartable_personal_states_company_id_user_id_is_starred_runt~",
                schema: "barang",
                table: "cartable_personal_states",
                columns: new[] { "company_id", "user_id", "is_starred", "runtime_task_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cartable_personal_states_company_id_user_id_runtime_task_id",
                schema: "barang",
                table: "cartable_personal_states",
                columns: new[] { "company_id", "user_id", "runtime_task_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cartable_personal_states",
                schema: "barang");
        }
    }
}
