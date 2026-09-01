using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration arrays

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowEngineAndHumanTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_instances",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    provider_instance_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    current_stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    suspended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.id);
                    table.UniqueConstraint("AK_workflow_instances_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_instances_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_sla_policies",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    escalation_permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_sla_policies", x => x.id);
                    table.UniqueConstraint("AK_workflow_sla_policies_company_id_id", x => new { x.company_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "human_tasks",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stage = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    task_type_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    required_permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    outcome_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_human_tasks", x => x.id);
                    table.UniqueConstraint("AK_human_tasks_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_human_tasks_workflow_instances_company_id_workflow_instance~",
                        columns: x => new { x.company_id, x.workflow_instance_id },
                        principalSchema: "barang",
                        principalTable: "workflow_instances",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    human_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_history", x => x.id);
                    table.UniqueConstraint("AK_workflow_history_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_history_workflow_instances_company_id_workflow_ins~",
                        columns: x => new { x.company_id, x.workflow_instance_id },
                        principalSchema: "barang",
                        principalTable: "workflow_instances",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_reconciliation_issues",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_reconciliation_issues", x => x.id);
                    table.UniqueConstraint("AK_workflow_reconciliation_issues_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_reconciliation_issues_workflow_instances_company_i~",
                        columns: x => new { x.company_id, x.workflow_instance_id },
                        principalSchema: "barang",
                        principalTable: "workflow_instances",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_escalations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    human_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    escalated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    target_permission_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_escalations", x => x.id);
                    table.UniqueConstraint("AK_workflow_escalations_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_escalations_human_tasks_company_id_human_task_id",
                        columns: x => new { x.company_id, x.human_task_id },
                        principalSchema: "barang",
                        principalTable: "human_tasks",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000056"), "workflow.read", "workflow", "workflow.read" },
                    { new Guid("00000000-0000-0000-0000-000000000057"), "workflow.task.claim", "workflow", "workflow.task.claim" },
                    { new Guid("00000000-0000-0000-0000-000000000058"), "workflow.task.complete", "workflow", "workflow.task.complete" },
                    { new Guid("00000000-0000-0000-0000-000000000059"), "workflow.manage", "workflow", "workflow.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_human_tasks_company_id_claimed_by_user_id_status",
                schema: "barang",
                table: "human_tasks",
                columns: new[] { "company_id", "claimed_by_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_human_tasks_company_id_status_due_at_utc",
                schema: "barang",
                table: "human_tasks",
                columns: new[] { "company_id", "status", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_human_tasks_company_id_workflow_instance_id_stage",
                schema: "barang",
                table: "human_tasks",
                columns: new[] { "company_id", "workflow_instance_id", "stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_escalations_company_id_human_task_id_level",
                schema: "barang",
                table: "workflow_escalations",
                columns: new[] { "company_id", "human_task_id", "level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_history_company_id_workflow_instance_id_occurred_a~",
                schema: "barang",
                table: "workflow_history",
                columns: new[] { "company_id", "workflow_instance_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_company_id_order_id",
                schema: "barang",
                table: "workflow_instances",
                columns: new[] { "company_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_instances_company_id_status_current_stage",
                schema: "barang",
                table: "workflow_instances",
                columns: new[] { "company_id", "status", "current_stage" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_reconciliation_issues_company_id_workflow_instance~",
                schema: "barang",
                table: "workflow_reconciliation_issues",
                columns: new[] { "company_id", "workflow_instance_id", "issue_code", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_sla_policies_company_id_stage",
                schema: "barang",
                table: "workflow_sla_policies",
                columns: new[] { "company_id", "stage" },
                unique: true);
            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('workflow.read','workflow.task.claim','workflow.task.complete','workflow.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;
                ALTER TABLE barang.workflow_instances ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_instances_company_isolation ON barang.workflow_instances USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.human_tasks ENABLE ROW LEVEL SECURITY; CREATE POLICY human_tasks_company_isolation ON barang.human_tasks USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.workflow_sla_policies ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_sla_policies_company_isolation ON barang.workflow_sla_policies USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.workflow_escalations ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_escalations_company_isolation ON barang.workflow_escalations USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.workflow_history ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_history_company_isolation ON barang.workflow_history USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.workflow_reconciliation_issues ENABLE ROW LEVEL SECURITY; CREATE POLICY workflow_reconciliation_issues_company_isolation ON barang.workflow_reconciliation_issues USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.workflow_instances,barang.human_tasks,barang.workflow_sla_policies,barang.workflow_reconciliation_issues TO barang_app; GRANT SELECT,INSERT ON barang.workflow_escalations,barang.workflow_history TO barang_app; REVOKE DELETE ON barang.workflow_instances,barang.human_tasks,barang.workflow_sla_policies,barang.workflow_escalations,barang.workflow_history,barang.workflow_reconciliation_issues FROM barang_app; REVOKE UPDATE ON barang.workflow_escalations,barang.workflow_history FROM barang_app; END IF;
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.workflow_instances,barang.human_tasks,barang.workflow_sla_policies,barang.workflow_reconciliation_issues TO barang_test_app; GRANT SELECT,INSERT ON barang.workflow_escalations,barang.workflow_history TO barang_test_app; REVOKE DELETE ON barang.workflow_instances,barang.human_tasks,barang.workflow_sla_policies,barang.workflow_escalations,barang.workflow_history,barang.workflow_reconciliation_issues FROM barang_test_app; REVOKE UPDATE ON barang.workflow_escalations,barang.workflow_history FROM barang_test_app; END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000056','00000000-0000-0000-0000-000000000057','00000000-0000-0000-0000-000000000058','00000000-0000-0000-0000-000000000059');");
            migrationBuilder.DropTable(
                name: "workflow_escalations",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_reconciliation_issues",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_sla_policies",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "human_tasks",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_instances",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000056"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000057"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000058"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000059"));
        }
    }
}
