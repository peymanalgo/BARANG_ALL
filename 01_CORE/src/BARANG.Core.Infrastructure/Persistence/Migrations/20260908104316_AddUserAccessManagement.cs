using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF migration metadata requires inline constant arrays.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccessManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "barang",
                table: "roles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "scope",
                schema: "barang",
                table: "roles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Company");

            migrationBuilder.AddColumn<int>(
                name: "access_failed_count",
                schema: "barang",
                table: "application_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at_utc",
                schema: "barang",
                table: "application_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "lockout_end_utc",
                schema: "barang",
                table: "application_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "security_stamp",
                schema: "barang",
                table: "application_users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE barang.roles
                SET scope = CASE WHEN is_system THEN 'System' ELSE 'Company' END;

                UPDATE barang.application_users
                SET security_stamp = md5(id::text || clock_timestamp()::text)
                WHERE security_stamp = '';

                INSERT INTO barang.persons
                    (id, company_id, personnel_code, first_name, last_name, display_name,
                     national_id, birth_date, email, phone, address, notes, is_active,
                     created_at_utc, updated_at_utc, version)
                SELECT
                    md5(u.id::text || '-barang-person')::uuid,
                    membership.company_id,
                    'LEGACY-' || left(replace(u.id::text, '-', ''), 24),
                    left(COALESCE(NULLIF(split_part(u.display_name, ' ', 1), ''), u.username), 100),
                    left(CASE WHEN position(' ' in u.display_name) > 0
                              THEN substring(u.display_name from position(' ' in u.display_name) + 1)
                              ELSE 'Account' END, 100),
                    left(COALESCE(NULLIF(u.display_name, ''), u.username), 201),
                    NULL, NULL, u.email, NULL, NULL, 'Migrated user account profile',
                    u.account_state = 'Active', u.created_at_utc, u.updated_at_utc, 1
                FROM barang.application_users u
                JOIN LATERAL (
                    SELECT uc.company_id
                    FROM barang.user_companies uc
                    WHERE uc.user_id = u.id
                    ORDER BY (uc.valid_to_utc IS NULL) DESC, uc.valid_from_utc
                    LIMIT 1
                ) membership ON true
                WHERE u.person_id IS NULL
                  AND NOT EXISTS (SELECT 1 FROM barang.api_clients c WHERE c.id = u.id)
                  AND NOT EXISTS (SELECT 1 FROM barang.persons p WHERE p.id = md5(u.id::text || '-barang-person')::uuid);

                UPDATE barang.application_users u
                SET person_id = md5(u.id::text || '-barang-person')::uuid
                WHERE u.person_id IS NULL
                  AND NOT EXISTS (SELECT 1 FROM barang.api_clients c WHERE c.id = u.id)
                  AND EXISTS (SELECT 1 FROM barang.persons p WHERE p.id = md5(u.id::text || '-barang-person')::uuid);
                """);

            migrationBuilder.CreateTable(
                name: "user_permissions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_permissions_application_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_permissions_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "barang",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000071"), "security.user.read", "security", "security.user.read" },
                    { new Guid("00000000-0000-0000-0000-000000000072"), "security.user.create", "security", "security.user.create" },
                    { new Guid("00000000-0000-0000-0000-000000000073"), "security.user.edit", "security", "security.user.edit" },
                    { new Guid("00000000-0000-0000-0000-000000000074"), "security.user.password.reset", "security", "security.user.password.reset" },
                    { new Guid("00000000-0000-0000-0000-000000000075"), "security.user.status.manage", "security", "security.user.status.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000076"), "security.user.role.manage", "security", "security.user.role.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000077"), "security.role.read", "security", "security.role.read" },
                    { new Guid("00000000-0000-0000-0000-000000000078"), "security.permission.read", "security", "security.permission.read" },
                    { new Guid("00000000-0000-0000-0000-000000000079"), "security.role.permission.manage", "security", "security.role.permission.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000080"), "security.user.permission.manage", "security", "security.user.permission.manage" }
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_application_users_access_failed_nonnegative",
                schema: "barang",
                table: "application_users",
                sql: "access_failed_count >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_company_id_user_id",
                schema: "barang",
                table: "user_permissions",
                columns: new[] { "company_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_permissions_permission_id",
                schema: "barang",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_permissions_user_id_permission_id_company_id",
                schema: "barang",
                table: "user_permissions",
                columns: new[] { "user_id", "permission_id", "company_id" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE barang.user_permissions ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.user_permissions FORCE ROW LEVEL SECURITY;
                CREATE POLICY user_permissions_company_isolation
                    ON barang.user_permissions
                    USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                    WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY user_permissions_authenticated_self_read
                    ON barang.user_permissions FOR SELECT
                    USING (user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid
                           AND company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                GRANT USAGE ON SCHEMA barang TO barang_app;
                GRANT SELECT, INSERT, DELETE ON barang.user_permissions TO barang_app;

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text || '-uam')::uuid, r.id, p.id, now(), NULL
                FROM barang.roles r
                CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.id BETWEEN '00000000-0000-0000-0000-000000000071'::uuid AND '00000000-0000-0000-0000-000000000080'::uuid
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(legacy.role_id::text || target.id::text || '-uam-user')::uuid,
                       legacy.role_id, target.id, now(), NULL
                FROM barang.role_permissions legacy
                JOIN barang.permissions old_permission ON old_permission.id = legacy.permission_id AND old_permission.code = 'security.user.manage'
                CROSS JOIN barang.permissions target
                WHERE target.code IN (
                    'security.user.read', 'security.user.create', 'security.user.edit',
                    'security.user.password.reset', 'security.user.status.manage',
                    'security.user.role.manage', 'security.permission.read',
                    'security.user.permission.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(legacy.role_id::text || target.id::text || '-uam-role')::uuid,
                       legacy.role_id, target.id, now(), NULL
                FROM barang.role_permissions legacy
                JOIN barang.permissions old_permission ON old_permission.id = legacy.permission_id AND old_permission.code = 'security.role.manage'
                CROSS JOIN barang.permissions target
                WHERE target.code IN (
                    'security.role.read', 'security.permission.read',
                    'security.role.permission.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_permissions",
                schema: "barang");

            migrationBuilder.Sql("""
                DELETE FROM barang.role_permissions
                WHERE permission_id BETWEEN '00000000-0000-0000-0000-000000000071'::uuid
                                        AND '00000000-0000-0000-0000-000000000080'::uuid;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_application_users_access_failed_nonnegative",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000071"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000072"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000073"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000074"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000075"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000076"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000077"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000078"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000079"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000080"));

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "scope",
                schema: "barang",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "access_failed_count",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "last_login_at_utc",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "lockout_end_utc",
                schema: "barang",
                table: "application_users");

            migrationBuilder.DropColumn(
                name: "security_stamp",
                schema: "barang",
                table: "application_users");
        }
    }
}
