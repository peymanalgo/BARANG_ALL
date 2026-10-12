using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDomainReferencesAndCustomerChildren : Migration
    {
        private static readonly string[] ScopedCodeColumns = ["company_id", "code"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_lookup_types_code",
                schema: "barang",
                table: "lookup_types");

            migrationBuilder.AddColumn<Guid>(
                name: "legal_entity_type_id",
                schema: "barang",
                table: "legal_customer_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_descriptions",
                schema: "barang",
                table: "defect_types",
                type: "jsonb",
                nullable: true,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "reference_display_order",
                schema: "barang",
                table: "defect_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reference_names",
                schema: "barang",
                table: "defect_types",
                type: "jsonb",
                nullable: true,
                defaultValue: "{}");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_group_id",
                schema: "barang",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "price_group_id",
                schema: "barang",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_condition_id",
                schema: "barang",
                table: "customer_financial_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_term_id",
                schema: "barang",
                table: "customer_financial_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "preferred_payment_method_id",
                schema: "barang",
                table: "customer_financial_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_status_id",
                schema: "barang",
                table: "customer_financial_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contact_role_id",
                schema: "barang",
                table: "customer_contacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "account_name",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "account_type_code",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "account_type_id",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "bank_id",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "card_number",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "character varying(19)",
                maxLength: 19,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ownership_type",
                schema: "barang",
                table: "customer_bank_accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<Guid>(
                name: "address_type_id",
                schema: "barang",
                table: "customer_addresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "customer_addresses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_descriptions",
                schema: "barang",
                table: "artwork_tags",
                type: "jsonb",
                nullable: true,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "reference_display_order",
                schema: "barang",
                table: "artwork_tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reference_names",
                schema: "barang",
                table: "artwork_tags",
                type: "jsonb",
                nullable: true,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "uq_lookup_types_company_code",
                schema: "barang",
                table: "lookup_types",
                columns: ScopedCodeColumns,
                unique: true,
                filter: "company_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_lookup_types_global_code",
                schema: "barang",
                table: "lookup_types",
                column: "code",
                unique: true,
                filter: "company_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_legal_customer_profiles_legal_entity_type_id",
                schema: "barang",
                table: "legal_customer_profiles",
                column: "legal_entity_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_group_id",
                schema: "barang",
                table: "customers",
                column: "customer_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_price_group_id",
                schema: "barang",
                table: "customers",
                column: "price_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_financial_profiles_payment_condition_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "payment_condition_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_financial_profiles_payment_term_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "payment_term_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_financial_profiles_preferred_payment_method_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "preferred_payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_financial_profiles_tax_status_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "tax_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_contacts_contact_role_id",
                schema: "barang",
                table: "customer_contacts",
                column: "contact_role_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_bank_accounts_account_type_id",
                schema: "barang",
                table: "customer_bank_accounts",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_bank_accounts_bank_id",
                schema: "barang",
                table: "customer_bank_accounts",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_addresses_address_type_id",
                schema: "barang",
                table: "customer_addresses",
                column: "address_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customer_addresses_lookup_values_address_type_id",
                schema: "barang",
                table: "customer_addresses",
                column: "address_type_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_bank_accounts_lookup_values_account_type_id",
                schema: "barang",
                table: "customer_bank_accounts",
                column: "account_type_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_bank_accounts_lookup_values_bank_id",
                schema: "barang",
                table: "customer_bank_accounts",
                column: "bank_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_contacts_lookup_values_contact_role_id",
                schema: "barang",
                table: "customer_contacts",
                column: "contact_role_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_financial_profiles_lookup_values_payment_condition~",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "payment_condition_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_financial_profiles_lookup_values_payment_term_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "payment_term_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_financial_profiles_lookup_values_preferred_payment~",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "preferred_payment_method_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customer_financial_profiles_lookup_values_tax_status_id",
                schema: "barang",
                table: "customer_financial_profiles",
                column: "tax_status_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_lookup_values_customer_group_id",
                schema: "barang",
                table: "customers",
                column: "customer_group_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_customers_lookup_values_price_group_id",
                schema: "barang",
                table: "customers",
                column: "price_group_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_legal_customer_profiles_lookup_values_legal_entity_type_id",
                schema: "barang",
                table: "legal_customer_profiles",
                column: "legal_entity_type_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(ReferenceDataMigrationSql.Up);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This data-preserving reference backfill requires an explicit forward migration for rollback.");
        }
    }
}
