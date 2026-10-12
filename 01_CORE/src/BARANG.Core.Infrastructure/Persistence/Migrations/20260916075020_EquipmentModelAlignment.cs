using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentModelAlignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "file_id",
                schema: "barang",
                table: "supplier_contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "file_version_id",
                schema: "barang",
                table: "supplier_contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "acquisition_method_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asset_number",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asset_tag",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_person_id",
                schema: "barang",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "commissioning_date",
                schema: "barang",
                table: "equipment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "condition_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "criticality_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "expected_useful_life_months",
                schema: "barang",
                table: "equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lifecycle_status_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manufacturer_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "equipment",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ownership_type_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "physical_location",
                schema: "barang",
                table: "equipment",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "purchase_date",
                schema: "barang",
                table: "equipment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "purchase_price",
                schema: "barang",
                table: "equipment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purchase_reference",
                schema: "barang",
                table: "equipment",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "responsible_person_id",
                schema: "barang",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "retirement_date",
                schema: "barang",
                table: "equipment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retirement_reason_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subtype_code",
                schema: "barang",
                table: "equipment",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                schema: "barang",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "warehouse_id",
                schema: "barang",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_end",
                schema: "barang",
                table: "equipment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "warranty_notes",
                schema: "barang",
                table: "equipment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "warranty_provider",
                schema: "barang",
                table: "equipment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "warranty_start",
                schema: "barang",
                table: "equipment",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "equipment_assignments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: true),
                    physical_location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    responsible_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_assignments", x => x.id);
                    table.UniqueConstraint("ak_equipment_assignments_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_assignments_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_assignments_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_assignments_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_failures",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    failure_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    failure_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    failure_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    failure_cause_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    severity_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reported_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipment_status_after_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    downtime_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    downtime_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_failures", x => x.id);
                    table.UniqueConstraint("ak_equipment_failures_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_failures_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_failures_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_failures_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_inspections",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    inspection_date = table.Column<DateOnly>(type: "date", nullable: false),
                    next_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    performed_by_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: true),
                    certificate_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_inspections", x => x.id);
                    table.UniqueConstraint("ak_equipment_inspections_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_inspections_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_inspections_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_inspections_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_maintenance",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    preventive = table.Column<bool>(type: "boolean", nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    next_service_date = table.Column<DateOnly>(type: "date", nullable: true),
                    interval_days = table.Column<int>(type: "integer", nullable: true),
                    meter_reading = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    provider_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    technician_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_performed = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    parts_used = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    result_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_maintenance", x => x.id);
                    table.UniqueConstraint("ak_equipment_maintenance_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_maintenance_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_maintenance_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_maintenance_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_repairs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    failure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    repair_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    repair_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    external = table.Column<bool>(type: "boolean", nullable: false),
                    technician_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_provider_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_performed = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    labor_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    parts_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    result_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    warranty_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_repairs", x => x.id);
                    table.UniqueConstraint("ak_equipment_repairs_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_repairs_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_repairs_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_repairs_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipment_specifications",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    spec_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    spec_value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_specifications", x => x.id);
                    table.UniqueConstraint("ak_equipment_specifications_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_equipment_specifications_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_equipment_specifications_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_equipment_specifications_equipment_company_id_equipment_id",
                        columns: x => new { x.company_id, x.equipment_id },
                        principalSchema: "barang",
                        principalTable: "equipment",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_assignment_current",
                schema: "barang",
                table: "equipment_assignments",
                columns: new[] { "company_id", "equipment_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_assignments_equipment",
                schema: "barang",
                table: "equipment_assignments",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_failures_equipment",
                schema: "barang",
                table: "equipment_failures",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "uq_equipment_failure_number",
                schema: "barang",
                table: "equipment_failures",
                columns: new[] { "company_id", "failure_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipment_inspection_due",
                schema: "barang",
                table: "equipment_inspections",
                columns: new[] { "company_id", "equipment_id", "next_due_date" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_inspections_equipment",
                schema: "barang",
                table: "equipment_inspections",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_maintenance_due",
                schema: "barang",
                table: "equipment_maintenance",
                columns: new[] { "company_id", "equipment_id", "next_service_date" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_maintenance_equipment",
                schema: "barang",
                table: "equipment_maintenance",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_equipment_repairs_equipment",
                schema: "barang",
                table: "equipment_repairs",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "uq_equipment_repair_number",
                schema: "barang",
                table: "equipment_repairs",
                columns: new[] { "company_id", "repair_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipment_specifications_equipment",
                schema: "barang",
                table: "equipment_specifications",
                columns: new[] { "company_id", "equipment_id" });

            migrationBuilder.CreateIndex(
                name: "uq_equipment_specification_key",
                schema: "barang",
                table: "equipment_specifications",
                columns: new[] { "company_id", "equipment_id", "spec_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_assignments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "equipment_failures",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "equipment_inspections",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "equipment_maintenance",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "equipment_repairs",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "equipment_specifications",
                schema: "barang");

            migrationBuilder.DropColumn(
                name: "file_id",
                schema: "barang",
                table: "supplier_contracts");

            migrationBuilder.DropColumn(
                name: "file_version_id",
                schema: "barang",
                table: "supplier_contracts");

            migrationBuilder.DropColumn(
                name: "acquisition_method_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "asset_number",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "asset_tag",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "assigned_person_id",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "brand_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "category_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "commissioning_date",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "condition_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "criticality_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "expected_useful_life_months",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "lifecycle_status_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "manufacturer_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "ownership_type_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "physical_location",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "purchase_date",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "purchase_price",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "purchase_reference",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "responsible_person_id",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "retirement_date",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "retirement_reason_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "subtype_code",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "warehouse_id",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "warranty_end",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "warranty_notes",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "warranty_provider",
                schema: "barang",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "warranty_start",
                schema: "barang",
                table: "equipment");
        }
    }
}
