using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteOrganizationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "barang",
                table: "organizational_units",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "organizational_assignments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                schema: "barang",
                table: "companies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "barang",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "country_code",
                schema: "barang",
                table: "companies",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "default_currency_code",
                schema: "barang",
                table: "companies",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "economic_code",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "barang",
                table: "companies",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                schema: "barang",
                table: "companies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "mobile",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "companies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "barang",
                table: "companies",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "barang",
                table: "companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registration_number",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tax_id",
                schema: "barang",
                table: "companies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "website",
                schema: "barang",
                table: "companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address",
                schema: "barang",
                table: "branches",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                schema: "barang",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "barang",
                table: "branches",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mobile",
                schema: "barang",
                table: "branches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                schema: "barang",
                table: "branches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                schema: "barang",
                table: "branches",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "barang",
                table: "branches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                schema: "barang",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("UPDATE barang.companies SET legal_name = display_name WHERE legal_name = '';");

            migrationBuilder.CreateIndex(
                name: "uq_companies_economic_code",
                schema: "barang",
                table: "companies",
                column: "economic_code",
                unique: true,
                filter: "economic_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_companies_national_id",
                schema: "barang",
                table: "companies",
                column: "national_id",
                unique: true,
                filter: "national_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_companies_registration_number",
                schema: "barang",
                table: "companies",
                column: "registration_number",
                unique: true,
                filter: "registration_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_companies_economic_code",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "uq_companies_national_id",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "uq_companies_registration_number",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "organizational_units");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "organizational_assignments");

            migrationBuilder.DropColumn(
                name: "address",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "country_code",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "default_currency_code",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "economic_code",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "legal_name",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "mobile",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "national_id",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "province",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "registration_number",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "tax_id",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "website",
                schema: "barang",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "address",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "city",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "mobile",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "notes",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "phone",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "barang",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "province",
                schema: "barang",
                table: "branches");
        }
    }
}
