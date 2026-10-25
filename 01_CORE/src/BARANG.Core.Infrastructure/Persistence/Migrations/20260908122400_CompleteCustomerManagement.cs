using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteCustomerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_addresses_customers_customer_company",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_contacts_customers_customer_company",
                schema: "barang",
                table: "customer_contacts");

            migrationBuilder.DropIndex(
                name: "ix_customers_directory",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "uq_customers_company_national_id",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "uq_customers_company_registration_no",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "uq_customer_addresses_one_active_primary",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.RenameColumn(
                name: "tax_id",
                schema: "barang",
                table: "customers",
                newName: "economic_code");

            migrationBuilder.RenameColumn(
                name: "job_title",
                schema: "barang",
                table: "customer_contacts",
                newName: "position_title");

            migrationBuilder.RenameColumn(
                name: "is_primary",
                schema: "barang",
                table: "customer_addresses",
                newName: "is_default");

            migrationBuilder.AddColumn<string>(
                name: "customer_group_code",
                schema: "barang",
                table: "customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "price_group_code",
                schema: "barang",
                table: "customers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "sales_representative_person_id",
                schema: "barang",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_code",
                schema: "barang",
                table: "customers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "barang",
                table: "customer_contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "barang",
                table: "customer_contacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "customer_contacts",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city_code",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province_code",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipient_mobile",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_bank_accounts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    account_holder_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_bank_accounts", x => x.id);
                    table.CheckConstraint("ck_customer_bank_accounts_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_customer_bank_accounts_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_financial_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_term_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    payment_condition_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    preferred_payment_method_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    default_discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    financial_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tax_status_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    default_currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_financial_profiles", x => x.id);
                    table.CheckConstraint("ck_customer_financial_profiles_credit", "credit_limit >= 0");
                    table.CheckConstraint("ck_customer_financial_profiles_discount", "default_discount_percent >= 0 AND default_discount_percent <= 100");
                    table.CheckConstraint("ck_customer_financial_profiles_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_customer_financial_profiles_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "individual_customer_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    national_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    father_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    gender_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    birth_certificate_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    place_of_issue = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    occupation = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_individual_customer_profiles", x => x.id);
                    table.CheckConstraint("ck_individual_customer_profiles_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_individual_customer_profiles_customers_customer_id_company_~",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "legal_customer_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    national_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    registration_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    establishment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    legal_entity_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    business_activity = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    managing_director = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_customer_profiles", x => x.id);
                    table.CheckConstraint("ck_legal_customer_profiles_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_legal_customer_profiles_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.individual_customer_profiles
                    (id, customer_id, company_id, first_name, last_name, national_id, father_name, birth_date,
                     gender_code, birth_certificate_number, place_of_issue, occupation, created_at_utc, updated_at_utc, version)
                SELECT c.id, c.id, c.company_id,
                       COALESCE(NULLIF(BTRIM(c.first_name), ''), NULLIF(BTRIM(c.display_name), ''), 'Legacy'),
                       COALESCE(NULLIF(BTRIM(c.last_name), ''), NULLIF(BTRIM(c.display_name), ''), 'Legacy'),
                       c.national_id, NULL, bp.date_of_birth, NULL, NULL, NULL, NULL,
                       c.created_at_utc, c.updated_at_utc, 1
                  FROM barang.customers c
                  LEFT JOIN barang.customer_business_profiles bp ON bp.customer_id = c.id AND bp.company_id = c.company_id
                 WHERE c.customer_type = 'Person';

                INSERT INTO barang.legal_customer_profiles
                    (id, customer_id, company_id, legal_name, trade_name, national_id, registration_number,
                     establishment_date, legal_entity_type_code, business_activity, website, managing_director,
                     created_at_utc, updated_at_utc, version)
                SELECT c.id, c.id, c.company_id,
                       COALESCE(NULLIF(BTRIM(c.legal_name), ''), NULLIF(BTRIM(c.display_name), ''), 'Legacy'),
                       NULL, c.national_id, c.registration_no, NULL, NULL, NULL, NULL, NULL,
                       c.created_at_utc, c.updated_at_utc, 1
                  FROM barang.customers c
                 WHERE c.customer_type = 'Organization';

                INSERT INTO barang.customer_financial_profiles
                    (id, customer_id, company_id, credit_limit, payment_term_code, payment_condition_code,
                     preferred_payment_method_code, default_discount_percent, financial_status, tax_status_code,
                     default_currency_code, created_at_utc, updated_at_utc, version)
                SELECT c.id, c.id, c.company_id, bp.credit_limit,
                       CASE WHEN bp.payment_terms_days > 0 THEN 'net-' || bp.payment_terms_days::text ELSE NULL END,
                       NULL, NULL, 0, 'Normal', NULL, 'IRR', c.created_at_utc, bp.updated_at_utc, GREATEST(bp.version, 1)
                  FROM barang.customer_business_profiles bp
                  JOIN barang.customers c ON c.id = bp.customer_id AND c.company_id = bp.company_id;

                UPDATE barang.customers c
                   SET economic_code = COALESCE(bp.economic_code, c.economic_code),
                       notes = COALESCE(c.notes, bp.business_details),
                       status_code = CASE WHEN c.is_active THEN 'Active' ELSE 'Inactive' END,
                       customer_type = CASE c.customer_type WHEN 'Person' THEN 'Individual' WHEN 'Organization' THEN 'LegalEntity' ELSE c.customer_type END
                  FROM barang.customer_business_profiles bp
                 WHERE bp.customer_id = c.id AND bp.company_id = c.company_id;

                UPDATE barang.customers
                   SET status_code = CASE WHEN is_active THEN 'Active' ELSE 'Inactive' END,
                       customer_type = CASE customer_type WHEN 'Person' THEN 'Individual' WHEN 'Organization' THEN 'LegalEntity' ELSE customer_type END
                 WHERE status_code = '' OR customer_type IN ('Person', 'Organization');

                UPDATE barang.customer_contacts SET first_name = COALESCE(name, '') WHERE first_name = '';
                UPDATE barang.customer_addresses
                   SET province_code = CASE WHEN province IS NULL THEN NULL ELSE LEFT(NULLIF(REGEXP_REPLACE(LOWER(BTRIM(province)), '[^a-z0-9._-]+', '-', 'g'), ''), 80) END,
                       city_code = CASE WHEN city IS NULL THEN NULL ELSE LEFT(NULLIF(REGEXP_REPLACE(LOWER(BTRIM(city)), '[^a-z0-9._-]+', '-', 'g'), ''), 80) END;
                """);

            migrationBuilder.DropTable(name: "customer_business_profiles", schema: "barang");
            migrationBuilder.DropColumn(name: "first_name", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "is_active", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "last_name", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "legal_name", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "national_id", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "registration_no", schema: "barang", table: "customers");
            migrationBuilder.DropColumn(name: "name", schema: "barang", table: "customer_contacts");
            migrationBuilder.DropColumn(name: "city", schema: "barang", table: "customer_addresses");
            migrationBuilder.DropColumn(name: "province", schema: "barang", table: "customer_addresses");

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000081"), "customer.create", "customer", "customer.create" },
                    { new Guid("00000000-0000-0000-0000-000000000082"), "customer.edit", "customer", "customer.edit" },
                    { new Guid("00000000-0000-0000-0000-000000000083"), "customer.status.manage", "customer", "customer.status.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000084"), "customer.financial.read", "customer", "customer.financial.read" },
                    { new Guid("00000000-0000-0000-0000-000000000085"), "customer.financial.manage", "customer", "customer.financial.manage" }
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(rp.id::text || p.id::text)::uuid, rp.role_id, p.id, rp.assigned_at_utc, rp.assigned_by_user_id
                  FROM barang.role_permissions rp
                  JOIN barang.permissions legacy ON legacy.id = rp.permission_id AND legacy.code = 'customer.manage'
                  CROSS JOIN barang.permissions p
                 WHERE p.code IN ('customer.create','customer.edit','customer.status.manage','customer.financial.read','customer.financial.manage')
                   AND NOT EXISTS (SELECT 1 FROM barang.role_permissions existing WHERE existing.role_id = rp.role_id AND existing.permission_id = p.id);

                INSERT INTO barang.user_permissions (id, user_id, permission_id, company_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(up.id::text || p.id::text)::uuid, up.user_id, p.id, up.company_id, up.assigned_at_utc, up.assigned_by_user_id
                  FROM barang.user_permissions up
                  JOIN barang.permissions legacy ON legacy.id = up.permission_id AND legacy.code = 'customer.manage'
                  CROSS JOIN barang.permissions p
                 WHERE p.code IN ('customer.create','customer.edit','customer.status.manage','customer.financial.read','customer.financial.manage')
                   AND NOT EXISTS (SELECT 1 FROM barang.user_permissions existing WHERE existing.user_id = up.user_id AND existing.permission_id = p.id AND existing.company_id = up.company_id);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_customers_company_economic_code",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "economic_code" },
                filter: "economic_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_company_id_sales_representative_person_id",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "sales_representative_person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_directory",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "status_code", "customer_code" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_addresses_one_active_default",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "is_default" },
                unique: true,
                filter: "is_default AND is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customer_bank_accounts_company_id_customer_id_account_number",
                schema: "barang",
                table: "customer_bank_accounts",
                columns: new[] { "company_id", "customer_id", "account_number" },
                unique: true,
                filter: "account_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_bank_accounts_company_id_customer_id_iban",
                schema: "barang",
                table: "customer_bank_accounts",
                columns: new[] { "company_id", "customer_id", "iban" },
                unique: true,
                filter: "iban IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customer_bank_accounts_customer_id_company_id",
                schema: "barang",
                table: "customer_bank_accounts",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_bank_accounts_customer_id_is_default",
                schema: "barang",
                table: "customer_bank_accounts",
                columns: new[] { "customer_id", "is_default" },
                unique: true,
                filter: "is_default AND is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customer_financial_profiles_company_id_customer_id",
                schema: "barang",
                table: "customer_financial_profiles",
                columns: new[] { "company_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_financial_profiles_customer_id_company_id",
                schema: "barang",
                table: "customer_financial_profiles",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_individual_customer_profiles_company_id_customer_id",
                schema: "barang",
                table: "individual_customer_profiles",
                columns: new[] { "company_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_individual_customer_profiles_company_id_last_name_first_name",
                schema: "barang",
                table: "individual_customer_profiles",
                columns: new[] { "company_id", "last_name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "IX_individual_customer_profiles_company_id_national_id",
                schema: "barang",
                table: "individual_customer_profiles",
                columns: new[] { "company_id", "national_id" },
                unique: true,
                filter: "national_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_individual_customer_profiles_customer_id_company_id",
                schema: "barang",
                table: "individual_customer_profiles",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "IX_legal_customer_profiles_company_id_customer_id",
                schema: "barang",
                table: "legal_customer_profiles",
                columns: new[] { "company_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legal_customer_profiles_company_id_legal_name",
                schema: "barang",
                table: "legal_customer_profiles",
                columns: new[] { "company_id", "legal_name" });

            migrationBuilder.CreateIndex(
                name: "IX_legal_customer_profiles_company_id_national_id",
                schema: "barang",
                table: "legal_customer_profiles",
                columns: new[] { "company_id", "national_id" },
                unique: true,
                filter: "national_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_legal_customer_profiles_company_id_registration_number",
                schema: "barang",
                table: "legal_customer_profiles",
                columns: new[] { "company_id", "registration_number" },
                unique: true,
                filter: "registration_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_legal_customer_profiles_customer_id_company_id",
                schema: "barang",
                table: "legal_customer_profiles",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_customer_addresses_customers_customer_id_company_id",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "company_id" },
                principalSchema: "barang",
                principalTable: "customers",
                principalColumns: new[] { "id", "company_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_contacts_customers_customer_id_company_id",
                schema: "barang",
                table: "customer_contacts",
                columns: new[] { "customer_id", "company_id" },
                principalSchema: "barang",
                principalTable: "customers",
                principalColumns: new[] { "id", "company_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customers_sales_representative_company_person",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "sales_representative_person_id" },
                principalSchema: "barang",
                principalTable: "persons",
                principalColumns: new[] { "company_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                ALTER TABLE barang.individual_customer_profiles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.legal_customer_profiles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.customer_financial_profiles ENABLE ROW LEVEL SECURITY;
                ALTER TABLE barang.customer_bank_accounts ENABLE ROW LEVEL SECURITY;
                CREATE POLICY individual_customer_profiles_company_isolation ON barang.individual_customer_profiles
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY legal_customer_profiles_company_isolation ON barang.legal_customer_profiles
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY customer_financial_profiles_company_isolation ON barang.customer_financial_profiles
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                CREATE POLICY customer_bank_accounts_company_isolation ON barang.customer_bank_accounts
                  USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid)
                  WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.individual_customer_profiles, barang.legal_customer_profiles,
                      barang.customer_financial_profiles, barang.customer_bank_accounts TO barang_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000081','00000000-0000-0000-0000-000000000082','00000000-0000-0000-0000-000000000083','00000000-0000-0000-0000-000000000084','00000000-0000-0000-0000-000000000085');
                DELETE FROM barang.user_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000081','00000000-0000-0000-0000-000000000082','00000000-0000-0000-0000-000000000083','00000000-0000-0000-0000-000000000084','00000000-0000-0000-0000-000000000085');
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_customer_addresses_customers_customer_id_company_id",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_customer_contacts_customers_customer_id_company_id",
                schema: "barang",
                table: "customer_contacts");

            migrationBuilder.DropForeignKey(
                name: "fk_customers_sales_representative_company_person",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropTable(
                name: "customer_bank_accounts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customer_financial_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "individual_customer_profiles",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "legal_customer_profiles",
                schema: "barang");

            migrationBuilder.DropIndex(
                name: "ix_customers_company_economic_code",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_company_id_sales_representative_person_id",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_customers_directory",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "uq_customer_addresses_one_active_default",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000081"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000082"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000083"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000084"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000085"));

            migrationBuilder.DropColumn(
                name: "customer_group_code",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "price_group_code",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "sales_representative_person_id",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "status_code",
                schema: "barang",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "barang",
                table: "customer_contacts");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "barang",
                table: "customer_contacts");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "customer_contacts");

            migrationBuilder.DropColumn(
                name: "city_code",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "province_code",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DropColumn(
                name: "recipient_mobile",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.RenameColumn(
                name: "economic_code",
                schema: "barang",
                table: "customers",
                newName: "tax_id");

            migrationBuilder.RenameColumn(
                name: "position_title",
                schema: "barang",
                table: "customer_contacts",
                newName: "job_title");

            migrationBuilder.RenameColumn(
                name: "is_default",
                schema: "barang",
                table: "customer_addresses",
                newName: "is_primary");

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "barang",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "barang",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "barang",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                schema: "barang",
                table: "customers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                schema: "barang",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_no",
                schema: "barang",
                table: "customers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "barang",
                table: "customer_contacts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_business_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    economic_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_business_profiles", x => x.id);
                    table.UniqueConstraint("AK_customer_business_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_customer_business_profiles_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customers_directory",
                schema: "barang",
                table: "customers",
                columns: new[] { "company_id", "is_active", "customer_code" });

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

            migrationBuilder.CreateIndex(
                name: "uq_customer_addresses_one_active_primary",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "is_primary" },
                unique: true,
                filter: "is_primary AND is_active");

            migrationBuilder.CreateIndex(
                name: "IX_customer_business_profiles_company_id_customer_id",
                schema: "barang",
                table: "customer_business_profiles",
                columns: new[] { "company_id", "customer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_business_profiles_customer_id_company_id",
                schema: "barang",
                table: "customer_business_profiles",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_customer_addresses_customers_customer_company",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "customer_id", "company_id" },
                principalSchema: "barang",
                principalTable: "customers",
                principalColumns: new[] { "id", "company_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_contacts_customers_customer_company",
                schema: "barang",
                table: "customer_contacts",
                columns: new[] { "customer_id", "company_id" },
                principalSchema: "barang",
                principalTable: "customers",
                principalColumns: new[] { "id", "company_id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
