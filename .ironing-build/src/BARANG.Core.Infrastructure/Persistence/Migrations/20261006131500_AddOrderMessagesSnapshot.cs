using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderMessagesSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_messages",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: true),
                    runtime_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    node_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_messages_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_messages_company_id_order_id_created_at_utc_id",
                schema: "barang",
                table: "order_messages",
                columns: new[] { "company_id", "order_id", "created_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_messages_company_id_runtime_task_id_author_user_id",
                schema: "barang",
                table: "order_messages",
                columns: new[] { "company_id", "runtime_task_id", "author_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_messages",
                schema: "barang");
        }
    }
}
