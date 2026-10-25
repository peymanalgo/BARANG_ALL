using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowInstanceVersionOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_company_id_DefinitionVersionId",
                schema: "barang",
                table: "workflow_instances",
                columns: new[] { "company_id", "DefinitionVersionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_workflow_instances_workflow_definition_versions_company_id_~",
                schema: "barang",
                table: "workflow_instances",
                columns: new[] { "company_id", "DefinitionVersionId" },
                principalSchema: "barang",
                principalTable: "workflow_definition_versions",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_workflow_instances_workflow_definition_versions_company_id_~",
                schema: "barang",
                table: "workflow_instances");

            migrationBuilder.DropIndex(
                name: "IX_workflow_instances_company_id_DefinitionVersionId",
                schema: "barang",
                table: "workflow_instances");
        }
    }
}
