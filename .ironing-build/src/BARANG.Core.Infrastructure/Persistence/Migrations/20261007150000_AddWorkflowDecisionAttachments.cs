using Microsoft.EntityFrameworkCore.Migrations;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[Migration("20261007150000_AddWorkflowDecisionAttachments")]
[DbContext(typeof(CoreDbContext))]
public partial class AddWorkflowDecisionAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "workflow_decision_attachments", schema: "barang",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                company_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_id = table.Column<Guid>(type: "uuid", nullable: false),
                workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                runtime_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                workflow_history_id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                node_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                outcome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                file_id = table.Column<Guid>(type: "uuid", nullable: false),
                file_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_workflow_decision_attachments", x => x.id);
                table.ForeignKey("fk_workflow_decision_attachments_history", x => new { x.company_id, x.workflow_history_id }, "workflow_history", new[] { "company_id", "id" }, principalSchema: "barang", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_workflow_decision_attachments_instances", x => new { x.company_id, x.workflow_instance_id }, "workflow_instances", new[] { "company_id", "id" }, principalSchema: "barang", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_workflow_decision_attachments_orders", x => new { x.company_id, x.order_id }, "orders", new[] { "company_id", "id" }, principalSchema: "barang", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_workflow_decision_attachments_tasks", x => new { x.company_id, x.runtime_task_id }, "workflow_runtime_tasks", new[] { "company_id", "id" }, principalSchema: "barang", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_workflow_decision_attachments_file_versions", x => new { x.company_id, x.file_version_id }, "file_versions", new[] { "company_id", "id" }, principalSchema: "barang", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("uq_workflow_decision_attachments_task", "workflow_decision_attachments", new[] { "company_id", "runtime_task_id", "id" }, "barang", unique: true);
        migrationBuilder.CreateIndex("ix_workflow_decision_attachments_order", "workflow_decision_attachments", new[] { "company_id", "order_id", "created_at_utc" }, "barang");
        migrationBuilder.Sql("ALTER TABLE barang.workflow_decision_attachments ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_decision_attachments_company_isolation ON barang.workflow_decision_attachments USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid); GRANT SELECT, INSERT, UPDATE, DELETE ON barang.workflow_decision_attachments TO barang_app;");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("workflow_decision_attachments", "barang");
}
