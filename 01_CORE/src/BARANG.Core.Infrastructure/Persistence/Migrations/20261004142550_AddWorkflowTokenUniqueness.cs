using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTokenUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_workflow_execution_tokens_company_id_workflow_instance_id_d~",
                schema: "barang",
                table: "workflow_execution_tokens",
                newName: "IX_workflow_execution_tokens_company_id_workflow_instance_id_~1");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_execution_tokens_company_id_workflow_instance_id_d~",
                schema: "barang",
                table: "workflow_execution_tokens",
                columns: new[] { "company_id", "workflow_instance_id", "definition_node_id", "branch_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_workflow_execution_tokens_company_id_workflow_instance_id_d~",
                schema: "barang",
                table: "workflow_execution_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_workflow_execution_tokens_company_id_workflow_instance_id_~1",
                schema: "barang",
                table: "workflow_execution_tokens",
                newName: "IX_workflow_execution_tokens_company_id_workflow_instance_id_d~");
        }
    }
}
#pragma warning restore CA1861
