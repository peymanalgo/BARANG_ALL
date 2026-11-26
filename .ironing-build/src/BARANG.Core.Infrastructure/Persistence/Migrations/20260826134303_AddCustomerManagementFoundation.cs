using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration metadata uses inline arrays.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    national_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    registration_no = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    primary_mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    primary_phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    primary_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.UniqueConstraint("ak_customers_id_company_id", x => new { x.id, x.company_id });
                    table.CheckConstraint("ck_customers_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_customers_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_addresses",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    province = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    city = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    district = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recipient_phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_addresses", x => x.id);
                    table.CheckConstraint("ck_customer_addresses_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_customer_addresses_customers_customer_company",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_contacts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    job_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_contacts", x => x.id);
                    table.CheckConstraint("ck_customer_contacts_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "fk_customer_contacts_customers_customer_company",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000011"), "customer.read", "customer", "customer.read" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), "customer.manage", "customer", "customer.manage" }
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator' AND p.code IN ('customer.read', 'customer.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_customer_addresses_customer_id_company_id",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_customer_order",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "company_id", "customer_id", "display_order", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_addresses_one_active_primary",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "is_primary" },
                unique: true,
                filter: "is_primary AND is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customer_contacts_customer_id_company_id",
                schema: "barang",
                table: "customer_contacts",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_contacts_customer_order",
                schema: "barang",
                table: "customer_contacts",
                columns: new[] { "company_id", "customer_id", "display_order", "id" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_contacts_one_active_primary",
                schema: "barang",
                table: "customer_contacts",
                columns: new[] { "customer_id", "is_primary" },
                unique: true,
                filter: "is_primary AND is_active");

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_display_name",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_email",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "primary_email" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_mobile",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "primary_mobile" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_directory",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "is_active", "customer_code" });

            migrationBuilder.CreateIndex(
                name: "uq_customers_company_code",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "customer_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customers_company_national_id",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "national_id" },
                unique: true,
                filter: "national_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_customers_company_registration_no",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "registration_no" },
                unique: true,
                filter: "registration_no IS NOT NULL");

            migrationBuilder.Sql("""
                ALTER TABLE barang.customers ENABLE ROW LEVEL SECURITY;
                CREATE POLICY customers_company_isolation ON barang.customers
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.customer_contacts ENABLE ROW LEVEL SECURITY;
                CREATE POLICY customer_contacts_company_isolation ON barang.customer_contacts
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.customer_addresses ENABLE ROW LEVEL SECURITY;
                CREATE POLICY customer_addresses_company_isolation ON barang.customer_addresses
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.customers, barang.customer_contacts, barang.customer_addresses TO barang_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000011', '00000000-0000-0000-0000-000000000012');");
            migrationBuilder.DropTable(
                name: "customer_addresses",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customer_contacts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000012"));
        }
    }
}
