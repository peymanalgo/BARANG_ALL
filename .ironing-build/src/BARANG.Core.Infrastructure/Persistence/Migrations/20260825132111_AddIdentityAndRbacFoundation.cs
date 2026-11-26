using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndRbacFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_clients",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    credential_fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    credential_expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_clients", x => x.id);
                    table.CheckConstraint("ck_api_clients_version_positive", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "application_users",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    account_state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_application_users", x => x.id);
                    table.CheckConstraint("ck_application_users_version_positive", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    technical_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    module = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                    table.CheckConstraint("ck_roles_version_positive", "version > 0");
                });

            migrationBuilder.CreateTable(
                name: "security_audit_events",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_client_companies",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client_companies", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_client_companies_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalSchema: "barang",
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_api_client_companies_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "authentication_sessions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authentication_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_authentication_sessions_application_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_companies",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    granted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_companies", x => x.id);
                    table.CheckConstraint("ck_user_companies_valid_period", "valid_to_utc IS NULL OR valid_to_utc > valid_from_utc");
                    table.ForeignKey(
                        name: "fk_user_companies_application_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_companies_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_client_permissions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_client_permissions_api_clients_api_client_id",
                        column: x => x.api_client_id,
                        principalSchema: "barang",
                        principalTable: "api_clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_api_client_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "barang",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalSchema: "barang",
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "barang",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => x.id);
                    table.CheckConstraint("ck_user_roles_valid_period", "valid_to_utc IS NULL OR valid_to_utc > valid_from_utc");
                    table.ForeignKey(
                        name: "fk_user_roles_application_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "barang",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "organization.company.manage", "organization", "organization.company.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "organization.branch.manage", "organization", "organization.branch.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "organization.settings.manage", "organization", "organization.settings.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "organization.unit.manage", "organization", "organization.unit.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "organization.warehouse.manage", "organization", "organization.warehouse.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "security.user.manage", "security", "security.user.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "security.role.manage", "security", "security.role.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_client_companies_company_id",
                schema: "barang",
                table: "api_client_companies",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_api_client_companies_api_client_id_company_id",
                schema: "barang",
                table: "api_client_companies",
                columns: new[] { "api_client_id", "company_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_client_permissions_permission_id",
                schema: "barang",
                table: "api_client_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_api_client_permissions_api_client_id_permission_id",
                schema: "barang",
                table: "api_client_permissions",
                columns: new[] { "api_client_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_api_clients_code",
                schema: "barang",
                table: "api_clients",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_application_users_username",
                schema: "barang",
                table: "application_users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_authentication_sessions_user_id",
                schema: "barang",
                table: "authentication_sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_authentication_sessions_token_hash",
                schema: "barang",
                table: "authentication_sessions",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_permissions_code",
                schema: "barang",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                schema: "barang",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "uq_role_permissions_role_id_permission_id",
                schema: "barang",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_roles_code",
                schema: "barang",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_occurred_at_utc",
                schema: "barang",
                table: "security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_user_companies_company_id_user_id",
                schema: "barang",
                table: "user_companies",
                columns: new[] { "company_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ux_user_companies_user_id_company_id_open",
                schema: "barang",
                table: "user_companies",
                columns: new[] { "user_id", "company_id" },
                unique: true,
                filter: "valid_to_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_company_id_user_id",
                schema: "barang",
                table: "user_roles",
                columns: new[] { "company_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "barang",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_roles_user_id_role_id_company_id_open",
                schema: "barang",
                table: "user_roles",
                columns: new[] { "user_id", "role_id", "company_id" },
                unique: true,
                filter: "valid_to_utc IS NULL");

            migrationBuilder.Sql("""
                ALTER TABLE barang.user_companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.user_companies FORCE ROW LEVEL SECURITY;
                CREATE POLICY user_companies_company_isolation ON barang.user_companies USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.user_roles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.user_roles FORCE ROW LEVEL SECURITY;
                CREATE POLICY user_roles_company_isolation ON barang.user_roles USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.api_client_companies ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.api_client_companies FORCE ROW LEVEL SECURITY;
                CREATE POLICY api_client_companies_company_isolation ON barang.api_client_companies USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_client_companies",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "api_client_permissions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "authentication_sessions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "security_audit_events",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "user_companies",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "api_clients",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "application_users",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "barang");
        }
    }
}
#pragma warning restore CA1861
