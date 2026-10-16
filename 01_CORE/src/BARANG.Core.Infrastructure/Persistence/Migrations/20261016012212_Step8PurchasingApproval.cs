using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Step8PurchasingApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalReason",
                schema: "barang",
                table: "purchase_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAtUtc",
                schema: "barang",
                table: "purchase_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "barang",
                table: "purchase_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedVersion",
                schema: "barang",
                table: "purchase_requests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalReason",
                schema: "barang",
                table: "purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAtUtc",
                schema: "barang",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                schema: "barang",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedVersion",
                schema: "barang",
                table: "purchase_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "barang",
                table: "purchase_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalReason",
                schema: "barang",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "barang",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "barang",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "ApprovedVersion",
                schema: "barang",
                table: "purchase_requests");

            migrationBuilder.DropColumn(
                name: "ApprovalReason",
                schema: "barang",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "barang",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "barang",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "ApprovedVersion",
                schema: "barang",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "barang",
                table: "purchase_orders");
        }
    }
}
