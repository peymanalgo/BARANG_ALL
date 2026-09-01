using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityControlAndReproduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reproduction_depth",
                schema: "barang",
                table: "production_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "reproduction_request_id",
                schema: "barang",
                table: "production_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "root_production_job_id",
                schema: "barang",
                table: "production_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_production_job_id",
                schema: "barang",
                table: "production_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "defect_types",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defect_types", x => x.id);
                    table.UniqueConstraint("AK_defect_types_company_id_id", x => new { x.company_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "qc_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "qc_templates",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    applicable_job_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    product_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_templates", x => x.id);
                    table.UniqueConstraint("AK_qc_templates_company_id_id", x => new { x.company_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "qc_inspections",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_version = table.Column<long>(type: "bigint", nullable: false),
                    inspector_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parent_inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reproduction_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspections", x => x.id);
                    table.UniqueConstraint("AK_qc_inspections_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_qc_inspections_order_items_company_id_order_item_id",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspections_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspections_production_jobs_company_id_production_job_id",
                        columns: x => new { x.company_id, x.production_job_id },
                        principalSchema: "barang",
                        principalTable: "production_jobs",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspections_qc_inspections_company_id_parent_inspection_~",
                        columns: x => new { x.company_id, x.parent_inspection_id },
                        principalSchema: "barang",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_inspections_qc_templates_company_id_template_id",
                        columns: x => new { x.company_id, x.template_id },
                        principalSchema: "barang",
                        principalTable: "qc_templates",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_template_criteria",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criterion_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    minimum_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    maximum_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    allowed_choice_codes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_template_criteria", x => x.id);
                    table.UniqueConstraint("AK_qc_template_criteria_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_qc_template_criteria_qc_templates_company_id_template_id",
                        columns: x => new { x.company_id, x.template_id },
                        principalSchema: "barang",
                        principalTable: "qc_templates",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reproduction_requests",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_production_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    root_production_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reproduction_requests", x => x.id);
                    table.UniqueConstraint("AK_reproduction_requests_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_reproduction_requests_production_jobs_company_id_source_pro~",
                        columns: x => new { x.company_id, x.source_production_job_id },
                        principalSchema: "barang",
                        principalTable: "production_jobs",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reproduction_requests_qc_inspections_company_id_source_insp~",
                        columns: x => new { x.company_id, x.source_inspection_id },
                        principalSchema: "barang",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_criteria_results",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_criterion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterion_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    criterion_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    criterion_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    pass_fail_value = table.Column<bool>(type: "boolean", nullable: true),
                    numeric_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    text_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    choice_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_criteria_results", x => x.id);
                    table.UniqueConstraint("AK_qc_criteria_results_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_qc_criteria_results_qc_inspections_company_id_inspection_id",
                        columns: x => new { x.company_id, x.inspection_id },
                        principalSchema: "barang",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_criteria_results_qc_template_criteria_company_id_templat~",
                        columns: x => new { x.company_id, x.template_criterion_id },
                        principalSchema: "barang",
                        principalTable: "qc_template_criteria",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "qc_defects",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criteria_result_id = table.Column<Guid>(type: "uuid", nullable: true),
                    defect_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    evidence_file_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_defects", x => x.id);
                    table.ForeignKey(
                        name: "FK_qc_defects_defect_types_company_id_defect_type_id",
                        columns: x => new { x.company_id, x.defect_type_id },
                        principalSchema: "barang",
                        principalTable: "defect_types",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_defects_file_versions_company_id_evidence_file_version_id",
                        columns: x => new { x.company_id, x.evidence_file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_defects_qc_criteria_results_company_id_criteria_result_id",
                        columns: x => new { x.company_id, x.criteria_result_id },
                        principalSchema: "barang",
                        principalTable: "qc_criteria_results",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_qc_defects_qc_inspections_company_id_inspection_id",
                        columns: x => new { x.company_id, x.inspection_id },
                        principalSchema: "barang",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000047"), "qc.read", "qc", "qc.read" },
                    { new Guid("00000000-0000-0000-0000-000000000048"), "qc.inspect", "qc", "qc.inspect" },
                    { new Guid("00000000-0000-0000-0000-000000000049"), "qc.review", "qc", "qc.review" },
                    { new Guid("00000000-0000-0000-0000-000000000050"), "qc.manage", "qc", "qc.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000051"), "reproduction.manage", "reproduction", "reproduction.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_defect_types_company_id_code",
                schema: "barang",
                table: "defect_types",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_criteria_results_company_id_inspection_id_template_crite~",
                schema: "barang",
                table: "qc_criteria_results",
                columns: new[] { "company_id", "inspection_id", "template_criterion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_criteria_results_company_id_template_criterion_id",
                schema: "barang",
                table: "qc_criteria_results",
                columns: new[] { "company_id", "template_criterion_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_defects_company_id_criteria_result_id",
                schema: "barang",
                table: "qc_defects",
                columns: new[] { "company_id", "criteria_result_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_defects_company_id_defect_type_id",
                schema: "barang",
                table: "qc_defects",
                columns: new[] { "company_id", "defect_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_defects_company_id_evidence_file_version_id",
                schema: "barang",
                table: "qc_defects",
                columns: new[] { "company_id", "evidence_file_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_defects_company_id_inspection_id",
                schema: "barang",
                table: "qc_defects",
                columns: new[] { "company_id", "inspection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_history_company_id_entity_id_occurred_at_utc_id",
                schema: "barang",
                table: "qc_history",
                columns: new[] { "company_id", "entity_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_order_id",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_order_item_id",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_parent_inspection_id",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "parent_inspection_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_production_job_id",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "production_job_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_status_created_at_utc",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspections_company_id_template_id",
                schema: "barang",
                table: "qc_inspections",
                columns: new[] { "company_id", "template_id" });

            migrationBuilder.CreateIndex(
                name: "IX_qc_template_criteria_company_id_template_id_code",
                schema: "barang",
                table: "qc_template_criteria",
                columns: new[] { "company_id", "template_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qc_templates_company_id_code",
                schema: "barang",
                table: "qc_templates",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reproduction_requests_company_id_source_inspection_id",
                schema: "barang",
                table: "reproduction_requests",
                columns: new[] { "company_id", "source_inspection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reproduction_requests_company_id_source_production_job_id",
                schema: "barang",
                table: "reproduction_requests",
                columns: new[] { "company_id", "source_production_job_id" });

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('qc.read','qc.inspect','qc.review','qc.manage','reproduction.manage')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                ALTER TABLE barang.qc_templates ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_templates_company_isolation ON barang.qc_templates USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.qc_template_criteria ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_template_criteria_company_isolation ON barang.qc_template_criteria USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.qc_inspections ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_inspections_company_isolation ON barang.qc_inspections USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.qc_criteria_results ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_criteria_results_company_isolation ON barang.qc_criteria_results USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.defect_types ENABLE ROW LEVEL SECURITY; CREATE POLICY defect_types_company_isolation ON barang.defect_types USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.qc_defects ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_defects_company_isolation ON barang.qc_defects USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.reproduction_requests ENABLE ROW LEVEL SECURITY; CREATE POLICY reproduction_requests_company_isolation ON barang.reproduction_requests USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.qc_history ENABLE ROW LEVEL SECURITY; CREATE POLICY qc_history_company_isolation ON barang.qc_history USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.qc_templates,barang.qc_inspections,barang.defect_types,barang.reproduction_requests TO barang_app;GRANT SELECT,INSERT ON barang.qc_template_criteria,barang.qc_criteria_results,barang.qc_defects,barang.qc_history TO barang_app;REVOKE DELETE ON barang.qc_templates,barang.qc_template_criteria,barang.qc_inspections,barang.qc_criteria_results,barang.defect_types,barang.qc_defects,barang.reproduction_requests,barang.qc_history FROM barang_app;REVOKE UPDATE ON barang.qc_template_criteria,barang.qc_criteria_results,barang.qc_defects,barang.qc_history FROM barang_app;END IF;
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN GRANT SELECT,INSERT,UPDATE ON barang.qc_templates,barang.qc_inspections,barang.defect_types,barang.reproduction_requests TO barang_test_app;GRANT SELECT,INSERT ON barang.qc_template_criteria,barang.qc_criteria_results,barang.qc_defects,barang.qc_history TO barang_test_app;REVOKE DELETE ON barang.qc_templates,barang.qc_template_criteria,barang.qc_inspections,barang.qc_criteria_results,barang.defect_types,barang.qc_defects,barang.reproduction_requests,barang.qc_history FROM barang_test_app;REVOKE UPDATE ON barang.qc_template_criteria,barang.qc_criteria_results,barang.qc_defects,barang.qc_history FROM barang_test_app;END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000047','00000000-0000-0000-0000-000000000048','00000000-0000-0000-0000-000000000049','00000000-0000-0000-0000-000000000050','00000000-0000-0000-0000-000000000051');");
            migrationBuilder.DropTable(
                name: "qc_defects",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "qc_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "reproduction_requests",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "defect_types",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "qc_criteria_results",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "qc_inspections",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "qc_template_criteria",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "qc_templates",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000047"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000048"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000049"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000050"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000051"));

            migrationBuilder.DropColumn(
                name: "reproduction_depth",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropColumn(
                name: "reproduction_request_id",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropColumn(
                name: "root_production_job_id",
                schema: "barang",
                table: "production_jobs");

            migrationBuilder.DropColumn(
                name: "source_production_job_id",
                schema: "barang",
                table: "production_jobs");
        }
    }
}
