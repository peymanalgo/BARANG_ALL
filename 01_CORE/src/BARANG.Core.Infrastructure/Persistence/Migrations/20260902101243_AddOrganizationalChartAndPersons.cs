using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationalChartAndPersons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE barang.organizational_units u
                SET branch_id = (SELECT b.id FROM barang.branches b WHERE b.company_id = u.company_id ORDER BY b.is_active DESC, b.code LIMIT 1)
                WHERE u.branch_id IS NULL;
                DO $$ BEGIN IF EXISTS (SELECT 1 FROM barang.organizational_units WHERE branch_id IS NULL) THEN RAISE EXCEPTION 'Cannot enforce branch ownership: an organizational unit company has no branch'; END IF; END $$;
                """);
            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                schema: "barang",
                table: "organizational_units",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "person_id",
                schema: "barang",
                table: "application_users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "persons",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    personnel_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    national_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_persons", x => x.id);
                    table.UniqueConstraint("uq_persons_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_persons_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_persons_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizational_assignments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_assignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    position_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizational_assignments", x => x.id);
                    table.UniqueConstraint("AK_organizational_assignments_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_organizational_assignments_dates", "end_date IS NULL OR start_date IS NULL OR end_date >= start_date");
                    table.CheckConstraint("ck_organizational_assignments_not_own_manager", "manager_assignment_id IS NULL OR manager_assignment_id <> id");
                    table.CheckConstraint("ck_organizational_assignments_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_organizational_assignments_organizational_assignments_compa~",
                        columns: x => new { x.company_id, x.manager_assignment_id },
                        principalSchema: "barang",
                        principalTable: "organizational_assignments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organizational_assignments_organizational_units_company_id_~",
                        columns: x => new { x.company_id, x.organizational_unit_id },
                        principalSchema: "barang",
                        principalTable: "organizational_units",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_organizational_assignments_persons_company_id_person_id",
                        columns: x => new { x.company_id, x.person_id },
                        principalSchema: "barang",
                        principalTable: "persons",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_application_users_person_id",
                schema: "barang",
                table: "application_users",
                column: "person_id",
                unique: true,
                filter: "person_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_organizational_assignments_company_id_manager_assignment_id",
                schema: "barang",
                table: "organizational_assignments",
                columns: new[] { "company_id", "manager_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_organizational_assignments_company_id_organizational_unit_id",
                schema: "barang",
                table: "organizational_assignments",
                columns: new[] { "company_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ux_organizational_assignments_active_placement",
                schema: "barang",
                table: "organizational_assignments",
                columns: new[] { "company_id", "person_id", "organizational_unit_id" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_persons_company_id_display_name",
                schema: "barang",
                table: "persons",
                columns: new[] { "company_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "uq_persons_company_id_personnel_code",
                schema: "barang",
                table: "persons",
                columns: new[] { "company_id", "personnel_code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_application_users_persons_person_id",
                schema: "barang",
                table: "application_users",
                column: "person_id",
                principalSchema: "barang",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                ALTER TABLE barang.persons ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.persons FORCE ROW LEVEL SECURITY;
                CREATE POLICY persons_company_isolation ON barang.persons USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.organizational_assignments ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.organizational_assignments FORCE ROW LEVEL SECURITY;
                CREATE POLICY organizational_assignments_company_isolation ON barang.organizational_assignments USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                GRANT SELECT, INSERT, UPDATE ON barang.persons, barang.organizational_assignments TO barang_app;
                REVOKE DELETE ON barang.persons, barang.organizational_assignments FROM barang_app;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_application_users_persons_person_id",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DropTable(
                name: "organizational_assignments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "persons",
                schema: "barang");

            migrationBuilder.DropIndex(
                name: "ux_application_users_person_id",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "person_id",
                schema: "barang",
                table: "application_users");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                schema: "barang",
                table: "organizational_units",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
