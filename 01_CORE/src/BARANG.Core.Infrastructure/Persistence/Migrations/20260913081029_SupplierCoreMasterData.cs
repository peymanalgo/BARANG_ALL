using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupplierCoreMasterData : Migration
    {
        private static readonly string[] MigrationColumns1 = new[] { "company_id", "id" };
        private static readonly string[] MigrationColumns2 = new[] { "company_id", "national_id" };
        private static readonly string[] MigrationColumns3 = new[] { "company_id", "supplier_number" };
        private static readonly string[] MigrationColumns4 = new[] { "company_id", "supplier_id", "created_at_utc" };
        private static readonly string[] MigrationColumns5 = new[] { "company_id", "supplier_id" };
        private static readonly string[] MigrationColumns6 = new[] { "company_id", "supplier_id", "account_number" };
        private static readonly string[] MigrationColumns7 = new[] { "company_id", "supplier_id", "iban" };
        private static readonly string[] MigrationColumns8 = new[] { "company_id", "supplier_id", "category_id" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "business_activity",
                schema: "barang",
                table: "suppliers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "cooperation_start_date",
                schema: "barang",
                table: "suppliers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entity_type",
                schema: "barang",
                table: "suppliers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<DateOnly>(
                name: "establishment_date",
                schema: "barang",
                table: "suppliers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "barang",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "grade_id",
                schema: "barang",
                table: "suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "hold_reason_id",
                schema: "barang",
                table: "suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_blocked",
                schema: "barang",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_preferred",
                schema: "barang",
                table: "suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "barang",
                table: "suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "legal_entity_type_id",
                schema: "barang",
                table: "suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                schema: "barang",
                table: "suppliers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "occupation",
                schema: "barang",
                table: "suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "supplier_number",
                schema: "barang",
                table: "suppliers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "trade_name",
                schema: "barang",
                table: "suppliers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "supplier_addresses",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    province_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    city_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    full_address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    landline = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_addresses", x => x.id);
                    table.UniqueConstraint("ak_supplier_addresses_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_addresses_active_default", "NOT is_default OR is_active");
                    table.CheckConstraint("ck_supplier_addresses_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_addresses_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_addresses_lookup_values_address_type_id",
                        column: x => x.address_type_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_addresses_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: MigrationColumns1,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_bank_accounts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_id = table.Column<Guid>(type: "uuid", nullable: true),
                    account_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    account_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ownership_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_holder_names = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    account_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    card_number = table.Column<string>(type: "character varying(19)", maxLength: 19, nullable: true),
                    branch_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    branch_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_bank_accounts", x => x.id);
                    table.UniqueConstraint("ak_supplier_bank_accounts_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_bank_accounts_active_default", "NOT is_default OR is_active");
                    table.CheckConstraint("ck_supplier_bank_accounts_version_positive", "version > 0");
                    table.CheckConstraint("ck_supplier_bank_ownership", "ownership_type IN ('Individual','Joint')");
                    table.ForeignKey(
                        name: "FK_supplier_bank_accounts_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bank_accounts_lookup_values_account_type_id",
                        column: x => x.account_type_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bank_accounts_lookup_values_bank_id",
                        column: x => x.bank_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_bank_accounts_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: MigrationColumns1,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_category_assignments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_category_assignments", x => x.id);
                    table.UniqueConstraint("ak_supplier_category_assignments_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_category_assignments_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_category_assignments_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_category_assignments_lookup_values_category_id",
                        column: x => x.category_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_category_assignments_suppliers_company_id_supplier~",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: MigrationColumns1,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_contacts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    position_title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    landline = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    mobile = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    extension = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_contacts", x => x.id);
                    table.UniqueConstraint("ak_supplier_contacts_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_contacts_active_default", "NOT is_primary OR is_active");
                    table.CheckConstraint("ck_supplier_contacts_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_contacts_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_contacts_lookup_values_contact_role_id",
                        column: x => x.contact_role_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_contacts_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: MigrationColumns1,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_financial_profiles",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_condition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    default_lead_time_days = table.Column<int>(type: "integer", nullable: true),
                    base_discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    credit_purchase_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    purchase_credit_limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_method_id = table.Column<Guid>(type: "uuid", nullable: true),
                    freight_delivery_term_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_financial_profiles", x => x.id);
                    table.UniqueConstraint("ak_supplier_financial_profiles_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_supplier_financial_amounts", "(minimum_order_amount IS NULL OR minimum_order_amount>=0) AND purchase_credit_limit>=0 AND (default_lead_time_days IS NULL OR default_lead_time_days>=0)");
                    table.CheckConstraint("ck_supplier_financial_discount", "base_discount_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_supplier_financial_profiles_version_positive", "version > 0");
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_lookup_values_freight_delivery_~",
                        column: x => x.freight_delivery_term_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_lookup_values_payment_condition~",
                        column: x => x.payment_condition_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_lookup_values_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_lookup_values_payment_term_id",
                        column: x => x.payment_term_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_lookup_values_shipping_method_id",
                        column: x => x.shipping_method_id,
                        principalSchema: "barang",
                        principalTable: "lookup_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_financial_profiles_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: MigrationColumns1,
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_company_national_id",
                schema: "barang",
                table: "suppliers",
                columns: MigrationColumns2);

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_grade_id",
                schema: "barang",
                table: "suppliers",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_hold_reason_id",
                schema: "barang",
                table: "suppliers",
                column: "hold_reason_id");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_legal_entity_type_id",
                schema: "barang",
                table: "suppliers",
                column: "legal_entity_type_id");

            migrationBuilder.CreateIndex(
                name: "uq_suppliers_company_number",
                schema: "barang",
                table: "suppliers",
                columns: MigrationColumns3,
                unique: true,
                filter: "supplier_number > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_suppliers_core_entity_type",
                schema: "barang",
                table: "suppliers",
                sql: "entity_type IN ('Individual','LegalEntity')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_suppliers_core_status",
                schema: "barang",
                table: "suppliers",
                sql: "NOT (is_blocked AND is_active)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_suppliers_hold_reason",
                schema: "barang",
                table: "suppliers",
                sql: "NOT is_blocked OR hold_reason_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_addresses_address_type_id",
                schema: "barang",
                table: "supplier_addresses",
                column: "address_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_addresses_directory",
                schema: "barang",
                table: "supplier_addresses",
                columns: MigrationColumns4);

            migrationBuilder.CreateIndex(
                name: "uq_supplier_addresses_default",
                schema: "barang",
                table: "supplier_addresses",
                columns: MigrationColumns5,
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_bank_accounts_account_type_id",
                schema: "barang",
                table: "supplier_bank_accounts",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_bank_accounts_bank_id",
                schema: "barang",
                table: "supplier_bank_accounts",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_bank_accounts_directory",
                schema: "barang",
                table: "supplier_bank_accounts",
                columns: MigrationColumns4);

            migrationBuilder.CreateIndex(
                name: "uq_supplier_bank_account_number",
                schema: "barang",
                table: "supplier_bank_accounts",
                columns: MigrationColumns6,
                unique: true,
                filter: "account_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_bank_accounts_default",
                schema: "barang",
                table: "supplier_bank_accounts",
                columns: MigrationColumns5,
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "uq_supplier_bank_iban",
                schema: "barang",
                table: "supplier_bank_accounts",
                columns: MigrationColumns7,
                unique: true,
                filter: "iban IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_category_assignments_category_id",
                schema: "barang",
                table: "supplier_category_assignments",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_category_assignments_company_id_supplier_id_catego~",
                schema: "barang",
                table: "supplier_category_assignments",
                columns: MigrationColumns8,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_contacts_contact_role_id",
                schema: "barang",
                table: "supplier_contacts",
                column: "contact_role_id");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_contacts_directory",
                schema: "barang",
                table: "supplier_contacts",
                columns: MigrationColumns4);

            migrationBuilder.CreateIndex(
                name: "uq_supplier_contacts_default",
                schema: "barang",
                table: "supplier_contacts",
                columns: MigrationColumns5,
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_company_id_supplier_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                columns: MigrationColumns5,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_freight_delivery_term_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                column: "freight_delivery_term_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_payment_condition_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                column: "payment_condition_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_payment_method_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_payment_term_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                column: "payment_term_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_financial_profiles_shipping_method_id",
                schema: "barang",
                table: "supplier_financial_profiles",
                column: "shipping_method_id");

            migrationBuilder.AddForeignKey(
                name: "FK_suppliers_lookup_values_grade_id",
                schema: "barang",
                table: "suppliers",
                column: "grade_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_suppliers_lookup_values_hold_reason_id",
                schema: "barang",
                table: "suppliers",
                column: "hold_reason_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_suppliers_lookup_values_legal_entity_type_id",
                schema: "barang",
                table: "suppliers",
                column: "legal_entity_type_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(SupplierCoreMigrationSql.AfterSchema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Abort before generated drops: business numbers and reference IDs require a reviewed forward migration.
            migrationBuilder.Sql("DO $guard$ BEGIN RAISE EXCEPTION 'Supplier core data must be preserved; use a reviewed forward migration.'; END $guard$;");

            migrationBuilder.DropForeignKey(
                name: "FK_suppliers_lookup_values_grade_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_suppliers_lookup_values_hold_reason_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropForeignKey(
                name: "FK_suppliers_lookup_values_legal_entity_type_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropTable(
                name: "supplier_addresses",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_bank_accounts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_category_assignments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_contacts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "supplier_financial_profiles",
                schema: "barang");

            migrationBuilder.DropIndex(
                name: "ix_suppliers_company_national_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_grade_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_hold_reason_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_legal_entity_type_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "uq_suppliers_company_number",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_suppliers_core_entity_type",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_suppliers_core_status",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "ck_suppliers_hold_reason",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "business_activity",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "cooperation_start_date",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "entity_type",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "establishment_date",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "grade_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "hold_reason_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "is_blocked",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "is_preferred",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "legal_entity_type_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "national_id",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "occupation",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "supplier_number",
                schema: "barang",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "trade_name",
                schema: "barang",
                table: "suppliers");
        }
    }
}
