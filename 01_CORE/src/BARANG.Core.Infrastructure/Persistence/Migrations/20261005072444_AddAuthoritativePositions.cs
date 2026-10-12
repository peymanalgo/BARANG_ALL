using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthoritativePositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "positions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reports_to_position_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    semantic_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.UniqueConstraint("uq_positions_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_positions_not_own_manager", "reports_to_position_id IS NULL OR reports_to_position_id <> id");
                    table.CheckConstraint("ck_positions_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_positions_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_reports_to_company_id_position_id",
                        columns: x => new { x.company_id, x.reports_to_position_id },
                        principalSchema: "barang",
                        principalTable: "positions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_units_company_id_unit_id",
                        columns: x => new { x.company_id, x.organizational_unit_id },
                        principalSchema: "barang",
                        principalTable: "organizational_units",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "position_assignments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_position_assignments", x => x.id);
                    table.UniqueConstraint("uq_position_assignments_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_position_assignments_dates", "end_date IS NULL OR start_date IS NULL OR end_date >= start_date");
                    table.CheckConstraint("ck_position_assignments_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_position_assignments_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_position_assignments_persons_company_id_person_id",
                        columns: x => new { x.company_id, x.person_id },
                        principalSchema: "barang",
                        principalTable: "persons",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_position_assignments_positions_company_id_position_id",
                        columns: x => new { x.company_id, x.position_id },
                        principalSchema: "barang",
                        principalTable: "positions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_position_assignments_person_status",
                schema: "barang",
                table: "position_assignments",
                columns: new[] { "company_id", "person_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_position_assignments_position_status",
                schema: "barang",
                table: "position_assignments",
                columns: new[] { "company_id", "position_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_company_id_reports_to_position_id",
                schema: "barang",
                table: "positions",
                columns: new[] { "company_id", "reports_to_position_id" });

            migrationBuilder.CreateIndex(
                name: "ix_positions_company_unit_active",
                schema: "barang",
                table: "positions",
                columns: new[] { "company_id", "organizational_unit_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "uq_positions_company_id_code",
                schema: "barang",
                table: "positions",
                columns: new[] { "company_id", "code" },
                unique: true);

            // Preserve legacy organizational assignment identity. Existing workflow node/task
            // position_id values therefore remain valid while the new model becomes authoritative.
            migrationBuilder.Sql(@"
                INSERT INTO barang.positions
                    (id, company_id, organizational_unit_id, reports_to_position_id, code, title,
                     semantic_role, notes, is_active, created_at_utc, updated_at_utc, version)
                SELECT id, company_id, organizational_unit_id, NULL, position_code, position_title,
                       NULL, notes, is_active, created_at_utc, updated_at_utc, version
                FROM barang.organizational_assignments;

                UPDATE barang.positions p
                   SET reports_to_position_id = a.manager_assignment_id
                  FROM barang.organizational_assignments a
                 WHERE a.id = p.id
                   AND a.company_id = p.company_id
                   AND a.manager_assignment_id IS NOT NULL;

                INSERT INTO barang.position_assignments
                    (id, company_id, position_id, person_id, start_date, end_date, is_primary,
                     status, notes, created_at_utc, updated_at_utc, version)
                SELECT id, company_id, id, person_id, start_date, end_date, is_primary,
                       CASE WHEN is_active THEN 'Active' ELSE 'Ended' END,
                       notes, created_at_utc, updated_at_utc, version
                FROM barang.organizational_assignments;

                ALTER TABLE barang.positions ENABLE ROW LEVEL SECURITY;
                CREATE POLICY positions_company_isolation ON barang.positions
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.position_assignments ENABLE ROW LEVEL SECURITY;
                CREATE POLICY position_assignments_company_isolation ON barang.position_assignments
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "position_assignments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "positions",
                schema: "barang");
        }
    }
}
