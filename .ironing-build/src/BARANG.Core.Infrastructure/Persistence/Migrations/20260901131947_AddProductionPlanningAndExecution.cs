using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPlanningAndExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "production_jobs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    print_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    queue_position = table.Column<int>(type: "integer", nullable: false),
                    assigned_operator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_equipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    suspension_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    artwork_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity_completed = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    execution_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    generation_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_jobs", x => x.id);
                    table.UniqueConstraint("AK_production_jobs_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_production_jobs_plan", "planned_end_utc IS NULL OR planned_start_utc IS NOT NULL AND planned_end_utc > planned_start_utc");
                    table.CheckConstraint("ck_production_jobs_quantity", "quantity_completed IS NULL OR quantity_completed >= 0");
                    table.CheckConstraint("ck_production_jobs_queue", "queue_position >= 0");
                    table.ForeignKey(
                        name: "FK_production_jobs_application_users_assigned_operator_user_id",
                        column: x => x.assigned_operator_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_artwork_versions_company_id_artwork_version~",
                        columns: x => new { x.company_id, x.artwork_version_id },
                        principalSchema: "barang",
                        principalTable: "artwork_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_equipment_company_id_assigned_equipment_id",
                        columns: x => new { x.company_id, x.assigned_equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_file_versions_company_id_file_version_id",
                        columns: x => new { x.company_id, x.file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_order_items_company_id_order_item_id",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_order_print_operations_company_id_print_ope~",
                        columns: x => new { x.company_id, x.print_operation_id },
                        principalSchema: "barang",
                        principalTable: "order_print_operations",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_jobs_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_job_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_production_job_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_production_job_history_application_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_production_job_history_production_jobs_company_id_productio~",
                        columns: x => new { x.company_id, x.production_job_id },
                        principalSchema: "barang",
                        principalTable: "production_jobs",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000043"), "production.read", "production", "production.read" },
                    { new Guid("00000000-0000-0000-0000-000000000044"), "production.plan", "production", "production.plan" },
                    { new Guid("00000000-0000-0000-0000-000000000045"), "production.assign", "production", "production.assign" },
                    { new Guid("00000000-0000-0000-0000-000000000046"), "production.execute", "production", "production.execute" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_production_job_history_actor_user_id",
                schema: "barang",
                table: "production_job_history",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_job_history_company_id_production_job_id_occurre~",
                schema: "barang",
                table: "production_job_history",
                columns: new[] { "company_id", "production_job_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_assigned_operator_user_id",
                schema: "barang",
                table: "production_jobs",
                column: "assigned_operator_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_artwork_version_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "artwork_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_assigned_equipment_id_status",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "assigned_equipment_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_assigned_operator_user_id_status",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "assigned_operator_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_file_version_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "file_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_generation_key",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "generation_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_order_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_order_item_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_print_operation_id",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "print_operation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_production_jobs_company_id_status_priority_queue_position_p~",
                schema: "barang",
                table: "production_jobs",
                columns: new[] { "company_id", "status", "priority", "queue_position", "planned_start_utc" });

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('production.read','production.plan','production.assign','production.execute')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                ALTER TABLE barang.production_jobs ENABLE ROW LEVEL SECURITY;
                CREATE POLICY production_jobs_company_isolation ON barang.production_jobs USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.production_job_history ENABLE ROW LEVEL SECURITY;
                CREATE POLICY production_job_history_company_isolation ON barang.production_job_history USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.production_jobs TO barang_app;GRANT SELECT,INSERT ON barang.production_job_history TO barang_app;REVOKE DELETE ON barang.production_jobs,barang.production_job_history FROM barang_app;REVOKE UPDATE ON barang.production_job_history FROM barang_app;END IF;
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.production_jobs TO barang_test_app;GRANT SELECT,INSERT ON barang.production_job_history TO barang_test_app;REVOKE DELETE ON barang.production_jobs,barang.production_job_history FROM barang_test_app;REVOKE UPDATE ON barang.production_job_history FROM barang_test_app;END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000043','00000000-0000-0000-0000-000000000044','00000000-0000-0000-0000-000000000045','00000000-0000-0000-0000-000000000046');");
            migrationBuilder.DropTable(
                name: "production_job_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "production_jobs",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000046"));
        }
    }
}
