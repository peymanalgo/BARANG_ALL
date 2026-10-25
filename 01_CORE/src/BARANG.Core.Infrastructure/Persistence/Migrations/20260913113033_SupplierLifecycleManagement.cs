using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupplierLifecycleManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "supplier_contracts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    contract_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    contract_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_contracts", x => x.id);
                    table.UniqueConstraint("AK_supplier_contracts_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_contracts_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_contracts_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_evaluations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: true),
                    period_end = table.Column<DateOnly>(type: "date", nullable: true),
                    evaluator_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    result_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_evaluations", x => x.id);
                    table.UniqueConstraint("AK_supplier_evaluations_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_evaluations_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_evaluations_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_lifecycle_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quality_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    qc_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    held_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    held_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    hold_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_lifecycle_profiles", x => x.id);
                    table.UniqueConstraint("AK_supplier_lifecycle_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_lifecycle_profiles_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_lifecycle_profiles_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_quality_incidents",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    incident_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    corrective_action = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_date = table.Column<DateOnly>(type: "date", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_quality_incidents", x => x.id);
                    table.UniqueConstraint("AK_supplier_quality_incidents_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_quality_incidents_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_quality_incidents_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_documents",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    issuing_authority = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_documents", x => x.id);
                    table.UniqueConstraint("AK_supplier_documents_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_documents_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_documents_supplier_contracts_company_id_contract_id",
                        columns: x => new { x.company_id, x.contract_id },
                        principalSchema: "barang",
                        principalTable: "supplier_contracts",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_documents_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_evaluation_criteria",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criterion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_evaluation_criteria", x => x.id);
                    table.UniqueConstraint("AK_supplier_evaluation_criteria_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_supplier_evaluation_criteria_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_evaluation_criteria_supplier_evaluations_company_i~",
                        columns: x => new { x.company_id, x.evaluation_id },
                        principalSchema: "barang",
                        principalTable: "supplier_evaluations",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_company_id_contract_number",
                schema: "barang",
                table: "supplier_contracts",
                columns: new[] { "company_id", "contract_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contracts_company_id_supplier_id",
                schema: "barang",
                table: "supplier_contracts",
                columns: new[] { "company_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_documents_company_id_contract_id",
                schema: "barang",
                table: "supplier_documents",
                columns: new[] { "company_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_documents_company_id_supplier_id_expiry_date",
                schema: "barang",
                table: "supplier_documents",
                columns: new[] { "company_id", "supplier_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_evaluation_criteria_company_id_evaluation_id_crite~",
                schema: "barang",
                table: "supplier_evaluation_criteria",
                columns: new[] { "company_id", "evaluation_id", "criterion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_evaluations_company_id_supplier_id_evaluation_date",
                schema: "barang",
                table: "supplier_evaluations",
                columns: new[] { "company_id", "supplier_id", "evaluation_date" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_lifecycle_profiles_company_id_supplier_id",
                schema: "barang",
                table: "supplier_lifecycle_profiles",
                columns: new[] { "company_id", "supplier_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_quality_incidents_company_id_supplier_id_incident_~",
                schema: "barang",
                table: "supplier_quality_incidents",
                columns: new[] { "company_id", "supplier_id", "incident_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_documents",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_evaluation_criteria",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_lifecycle_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_quality_incidents",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_contracts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_evaluations",
                schema: "barang");
        }
    }
}
