using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs instances written by the pre-definition-runtime starter, which used
/// a provider status token in the WorkflowStage column. The canonical first
/// stage is AdministrativeReview.
/// </summary>
public partial class NormalizeWorkflowStartedStage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("update barang.workflow_instances set current_stage='AdministrativeReview' where current_stage='Started';");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("update barang.workflow_instances set current_stage='Started' where current_stage='AdministrativeReview';");
}
