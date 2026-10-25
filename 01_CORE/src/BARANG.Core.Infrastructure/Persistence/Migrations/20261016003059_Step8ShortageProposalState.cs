using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Step8ShortageProposalState : Migration
    {
        private static readonly string[] ShortageProposalIndexColumns = ["company_id", "purchase_request_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_rechecked_at_utc",
                schema: "barang",
                table: "stock_shortages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proposal_status",
                schema: "barang",
                table: "stock_shortages",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.Sql("UPDATE barang.stock_shortages SET proposal_status = 'Open' WHERE proposal_status = ''");

            migrationBuilder.AddColumn<Guid>(
                name: "purchase_request_id",
                schema: "barang",
                table: "stock_shortages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_shortages_company_id_purchase_request_id",
                schema: "barang",
                table: "stock_shortages",
                columns: ShortageProposalIndexColumns,
                unique: true,
                filter: "purchase_request_id IS NOT NULL");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_shortages_company_id_purchase_request_id",
                schema: "barang",
                table: "stock_shortages");

            migrationBuilder.DropColumn(
                name: "last_rechecked_at_utc",
                schema: "barang",
                table: "stock_shortages");

            migrationBuilder.DropColumn(
                name: "proposal_status",
                schema: "barang",
                table: "stock_shortages");

            migrationBuilder.DropColumn(
                name: "purchase_request_id",
                schema: "barang",
                table: "stock_shortages");
        }
    }
}
