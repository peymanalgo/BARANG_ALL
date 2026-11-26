using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialApprovalSnapshotReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "approved_price_snapshot_id",
                schema: "barang",
                table: "order_financial_states",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_financial_states_company_id_approved_price_snapshot_id",
                schema: "barang",
                table: "order_financial_states",
                columns: new[] { "company_id", "approved_price_snapshot_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_order_financial_states_order_price_snapshots_company_id_app~",
                schema: "barang",
                table: "order_financial_states",
                columns: new[] { "company_id", "approved_price_snapshot_id" },
                principalSchema: "barang",
                principalTable: "order_price_snapshots",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_financial_states_order_price_snapshots_company_id_app~",
                schema: "barang",
                table: "order_financial_states");

            migrationBuilder.DropIndex(
                name: "IX_order_financial_states_company_id_approved_price_snapshot_id",
                schema: "barang",
                table: "order_financial_states");

            migrationBuilder.DropColumn(
                name: "approved_price_snapshot_id",
                schema: "barang",
                table: "order_financial_states");
        }
    }
}
#pragma warning restore CA1861
