using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowRepeatedWorkflowStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_human_tasks_company_id_workflow_instance_id_stage",
                schema: "barang",
                table: "human_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_human_tasks_company_id_workflow_instance_id_stage",
                schema: "barang",
                table: "human_tasks",
                columns: new[] { "company_id", "workflow_instance_id", "stage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_human_tasks_company_id_workflow_instance_id_stage",
                schema: "barang",
                table: "human_tasks");

            migrationBuilder.CreateIndex(
                name: "IX_human_tasks_company_id_workflow_instance_id_stage",
                schema: "barang",
                table: "human_tasks",
                columns: new[] { "company_id", "workflow_instance_id", "stage" },
                unique: true);
        }
    }
}
