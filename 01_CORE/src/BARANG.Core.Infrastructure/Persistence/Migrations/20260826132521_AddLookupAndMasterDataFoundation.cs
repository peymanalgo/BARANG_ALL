using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration metadata uses inline column arrays.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupAndMasterDataFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lookup_types",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    allow_custom_values = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookup_types", x => x.id);
                    table.CheckConstraint("ck_lookup_types_scope_company", "(scope = 'Global' AND company_id IS NULL) OR (scope = 'Company' AND company_id IS NOT NULL)");
                    table.CheckConstraint("ck_lookup_types_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_lookup_types_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lookup_values",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookup_values", x => x.id);
                    table.CheckConstraint("ck_lookup_values_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_lookup_values_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lookup_values_lookup_types_lookup_type_id",
                        column: x => x.lookup_type_id,
                        principalSchema: "barang",
                        principalTable: "lookup_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lookup_value_translations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lookup_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lookup_value_translations", x => x.id);
                    table.CheckConstraint("ck_lookup_value_translations_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_lookup_value_translations_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_lookup_value_translations_lookup_values_lookup_value_id",
                        column: x => x.lookup_value_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000009"), "lookup.read", "lookup", "lookup.read" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), "lookup.manage", "lookup", "lookup.manage" }
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r
                CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.code IN ('lookup.read', 'lookup.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_lookup_types_company_id_is_active",
                schema: "barang",
                table: "lookup_types",
                columns: new[] { "company_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "uq_lookup_types_code",
                schema: "barang",
                table: "lookup_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lookup_value_translations_company_id",
                schema: "barang",
                table: "lookup_value_translations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_lookup_value_translations_value_culture",
                schema: "barang",
                table: "lookup_value_translations",
                columns: new[] { "lookup_value_id", "culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lookup_values_options",
                schema: "barang",
                table: "lookup_values",
                columns: new[] { "company_id", "lookup_type_id", "is_active", "display_order" });

            migrationBuilder.CreateIndex(
                name: "uq_lookup_values_lookup_type_id_code",
                schema: "barang",
                table: "lookup_values",
                columns: new[] { "lookup_type_id", "code" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE barang.lookup_types ENABLE ROW LEVEL SECURITY;
                CREATE POLICY lookup_types_company_read ON barang.lookup_types
                  FOR SELECT USING (company_id IS NULL OR company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY lookup_types_company_write ON barang.lookup_types
                  FOR ALL USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                ALTER TABLE barang.lookup_values ENABLE ROW LEVEL SECURITY;
                CREATE POLICY lookup_values_company_read ON barang.lookup_values
                  FOR SELECT USING (company_id IS NULL OR company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY lookup_values_company_write ON barang.lookup_values
                  FOR ALL USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                ALTER TABLE barang.lookup_value_translations ENABLE ROW LEVEL SECURITY;
                CREATE POLICY lookup_value_translations_company_read ON barang.lookup_value_translations
                  FOR SELECT USING (company_id IS NULL OR company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY lookup_value_translations_company_write ON barang.lookup_value_translations
                  FOR ALL USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.lookup_types, barang.lookup_values, barang.lookup_value_translations TO barang_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000009', '00000000-0000-0000-0000-000000000010');");
            migrationBuilder.DropTable(
                name: "lookup_value_translations",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "lookup_values",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "lookup_types",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"));
        }
    }
}
