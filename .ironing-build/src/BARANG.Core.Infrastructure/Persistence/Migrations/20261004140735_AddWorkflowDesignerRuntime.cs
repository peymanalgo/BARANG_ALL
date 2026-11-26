using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowDesignerRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefinitionVersionId",
                schema: "barang",
                table: "workflow_instances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "workflow_definitions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_json = table.Column<string>(type: "jsonb", nullable: false),
                    active_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definitions", x => x.id);
                    table.UniqueConstraint("AK_workflow_definitions_company_id_id", x => new { x.company_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "workflow_execution_tokens",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_execution_tokens", x => x.id);
                    table.UniqueConstraint("AK_workflow_execution_tokens_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_execution_tokens_workflow_instances_company_id_wor~",
                        columns: x => new { x.company_id, x.workflow_instance_id },
                        principalSchema: "barang",
                        principalTable: "workflow_instances",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definition_versions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    change_note_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition_versions", x => x.id);
                    table.UniqueConstraint("AK_workflow_definition_versions_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_definition_versions_workflow_definitions_company_i~",
                        columns: x => new { x.company_id, x.definition_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definitions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definition_nodes",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title_json = table.Column<string>(type: "jsonb", nullable: false),
                    description_json = table.Column<string>(type: "jsonb", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    x = table.Column<decimal>(type: "numeric", nullable: false),
                    y = table.Column<decimal>(type: "numeric", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition_nodes", x => x.id);
                    table.UniqueConstraint("AK_workflow_definition_nodes_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_definition_nodes_organizational_assignments_compan~",
                        columns: x => new { x.company_id, x.position_id },
                        principalSchema: "barang",
                        principalTable: "organizational_assignments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_definition_nodes_workflow_definition_versions_comp~",
                        columns: x => new { x.company_id, x.definition_version_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definition_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_definition_transitions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_definition_transitions", x => x.id);
                    table.UniqueConstraint("AK_workflow_definition_transitions_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_definition_transitions_workflow_definition_nodes_c~",
                        columns: x => new { x.company_id, x.from_node_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definition_nodes",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_definition_transitions_workflow_definition_nodes_~1",
                        columns: x => new { x.company_id, x.to_node_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definition_nodes",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_definition_transitions_workflow_definition_version~",
                        columns: x => new { x.company_id, x.definition_version_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definition_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_runtime_tasks",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_runtime_tasks", x => x.id);
                    table.UniqueConstraint("AK_workflow_runtime_tasks_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_workflow_runtime_tasks_organizational_assignments_company_i~",
                        columns: x => new { x.company_id, x.position_id },
                        principalSchema: "barang",
                        principalTable: "organizational_assignments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_runtime_tasks_workflow_definition_nodes_company_id~",
                        columns: x => new { x.company_id, x.definition_node_id },
                        principalSchema: "barang",
                        principalTable: "workflow_definition_nodes",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_runtime_tasks_workflow_instances_company_id_workfl~",
                        columns: x => new { x.company_id, x.workflow_instance_id },
                        principalSchema: "barang",
                        principalTable: "workflow_instances",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_nodes_company_id_definition_version_id_~",
                schema: "barang",
                table: "workflow_definition_nodes",
                columns: new[] { "company_id", "definition_version_id", "node_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_nodes_company_id_position_id",
                schema: "barang",
                table: "workflow_definition_nodes",
                columns: new[] { "company_id", "position_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_transitions_company_id_definition_versi~",
                schema: "barang",
                table: "workflow_definition_transitions",
                columns: new[] { "company_id", "definition_version_id", "from_node_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_transitions_company_id_from_node_id",
                schema: "barang",
                table: "workflow_definition_transitions",
                columns: new[] { "company_id", "from_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_transitions_company_id_to_node_id",
                schema: "barang",
                table: "workflow_definition_transitions",
                columns: new[] { "company_id", "to_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_company_id_definition_id_status",
                schema: "barang",
                table: "workflow_definition_versions",
                columns: new[] { "company_id", "definition_id", "status" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definition_versions_company_id_definition_id_versi~",
                schema: "barang",
                table: "workflow_definition_versions",
                columns: new[] { "company_id", "definition_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_definitions_company_id_code",
                schema: "barang",
                table: "workflow_definitions",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workflow_execution_tokens_company_id_workflow_instance_id_d~",
                schema: "barang",
                table: "workflow_execution_tokens",
                columns: new[] { "company_id", "workflow_instance_id", "definition_node_id", "branch_key", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_definition_node_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "definition_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_position_id",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "position_id" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_runtime_tasks_company_id_workflow_instance_id_stat~",
                schema: "barang",
                table: "workflow_runtime_tasks",
                columns: new[] { "company_id", "workflow_instance_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_definition_transitions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_execution_tokens",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_runtime_tasks",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_definition_nodes",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_definition_versions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "workflow_definitions",
                schema: "barang");

            migrationBuilder.DropColumn(
                name: "DefinitionVersionId",
                schema: "barang",
                table: "workflow_instances");
        }
    }
}
#pragma warning restore CA1861
