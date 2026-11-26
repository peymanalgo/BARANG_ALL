using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTaskClaimsAndOrderMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_runtime_tasks_company_id_position_id",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "claimed_at_utc",
                schema: "barang",
                table: "workflow_runtime_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "claimed_by_user_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_position_id_status_claime~",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "position_id", "status", "claimed_by_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_runtime_tasks_company_id_position_id_status_claime~",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.DropColumn(
                name: "claimed_at_utc",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.DropColumn(
                name: "claimed_by_user_id",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_position_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "position_id" });
        }
    }
}
