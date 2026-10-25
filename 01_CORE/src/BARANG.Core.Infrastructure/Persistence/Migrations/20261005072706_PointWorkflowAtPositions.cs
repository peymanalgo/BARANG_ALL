using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PointWorkflowAtPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_definition_nodes_organizational_assignments_compan~",
                schema: "barang",
                table: "workflow_definition_nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_runtime_tasks_organizational_assignments_company_i~",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_definition_nodes_positions_company_id_position_id",
                schema: "barang",
                table: "workflow_definition_nodes",
                columns: new[] { "company_id", "position_id" },
                principalSchema: "barang",
                principalTable: "positions",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_runtime_tasks_positions_company_id_position_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "position_id" },
                principalSchema: "barang",
                principalTable: "positions",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_definition_nodes_positions_company_id_position_id",
                schema: "barang",
                table: "workflow_definition_nodes");

            migrationBuilder.DropForeignKey(
                name: "FK_workflow_runtime_tasks_positions_company_id_position_id",
                schema: "barang",
                table: "workflow_runtime_tasks");

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_definition_nodes_organizational_assignments_compan~",
                schema: "barang",
                table: "workflow_definition_nodes",
                columns: new[] { "company_id", "position_id" },
                principalSchema: "barang",
                principalTable: "organizational_assignments",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_runtime_tasks_organizational_assignments_company_i~",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "position_id" },
                principalSchema: "barang",
                principalTable: "organizational_assignments",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
