using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "barang");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                    table.CheckConstraint("ck_companies_version_positive", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "branches",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                    table.UniqueConstraint("uq_branches_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_branches_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_branches_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_settings",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    value_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_settings", x => x.id);
                    table.CheckConstraint("ck_company_settings_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_company_settings_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organizational_units",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizational_units", x => x.id);
                    table.UniqueConstraint("uq_organizational_units_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_organizational_units_not_own_parent", "parent_id IS NULL OR parent_id <> id");
                    table.CheckConstraint("ck_organizational_units_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_organizational_units_branches_company_id_branch_id",
                        columns: x => new { x.company_id, x.branch_id },
                        principalSchema: "barang",
                        principalTable: "branches",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizational_units_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organizational_units_parent_company_id_parent_id",
                        columns: x => new { x.company_id, x.parent_id },
                        principalSchema: "barang",
                        principalTable: "organizational_units",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                    table.CheckConstraint("ck_warehouses_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_warehouses_branches_company_id_branch_id",
                        columns: x => new { x.company_id, x.branch_id },
                        principalSchema: "barang",
                        principalTable: "branches",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_warehouses_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "uq_branches_company_id_code",
                schema: "barang",
                table: "branches",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_companies_code",
                schema: "barang",
                table: "companies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_company_settings_company_id_key",
                schema: "barang",
                table: "company_settings",
                columns: new[] { "company_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_company_id_branch_id",
                schema: "barang",
                table: "organizational_units",
                columns: new[] { "company_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organizational_units_company_id_parent_id",
                schema: "barang",
                table: "organizational_units",
                columns: new[] { "company_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "uq_organizational_units_company_id_code",
                schema: "barang",
                table: "organizational_units",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_company_id_branch_id",
                schema: "barang",
                table: "warehouses",
                columns: new[] { "company_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "uq_warehouses_company_id_code",
                schema: "barang",
                table: "warehouses",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE barang.company_settings ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.company_settings FORCE ROW LEVEL SECURITY;
                CREATE POLICY company_settings_company_isolation ON barang.company_settings
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                ALTER TABLE barang.branches ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.branches FORCE ROW LEVEL SECURITY;
                CREATE POLICY branches_company_isolation ON barang.branches
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                ALTER TABLE barang.organizational_units ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.organizational_units FORCE ROW LEVEL SECURITY;
                CREATE POLICY organizational_units_company_isolation ON barang.organizational_units
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                ALTER TABLE barang.warehouses ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.warehouses FORCE ROW LEVEL SECURITY;
                CREATE POLICY warehouses_company_isolation ON barang.warehouses
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_settings",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "organizational_units",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "warehouses",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "branches",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "barang");
        }
    }
}
#pragma warning restore CA1861
