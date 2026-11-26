using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderServiceSelectionsAndDesignComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_service_selections",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_service_selections", x => x.id);
                    table.CheckConstraint("ck_order_service_selection_owner", "(order_item_id IS NULL) OR (order_item_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_order_service_selections_order_items_company_id_order_item_~",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_service_selections_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_service_selections_company_id_order_id_order_item_id_~",
                schema: "barang",
                table: "order_service_selections",
                columns: new[] { "company_id", "order_id", "order_item_id", "service_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_service_selections_company_id_order_item_id",
                schema: "barang",
                table: "order_service_selections",
                columns: new[] { "company_id", "order_item_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_service_selections",
                schema: "barang");
        }
    }
}
