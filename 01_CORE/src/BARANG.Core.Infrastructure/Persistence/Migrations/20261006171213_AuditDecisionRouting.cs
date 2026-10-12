using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditDecisionRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "origin_node_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_node_key",
                schema: "barang",
                table: "workflow_runtime_tasks",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_origin_node_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "origin_node_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_runtime_tasks_company_id_origin_node_id",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.DropColumn(
                name: "origin_node_id",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.DropColumn(
                name: "origin_node_key",
                schema: "barang",
                table: "workflow_runtime_tasks");
        }
    }
}
