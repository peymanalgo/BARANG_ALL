using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxEngineCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "component_type",
                schema: "barang",
                table: "tax_rules",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "barang",
                table: "tax_rules",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "IRR");

            migrationBuilder.AddColumn<string>(
                name: "customer_tax_status_code",
                schema: "barang",
                table: "tax_rules",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "barang",
                table: "tax_rules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "barang",
                table: "tax_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                schema: "barang",
                table: "tax_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                schema: "barang",
                table: "tax_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_code",
                schema: "barang",
                table: "tax_rules",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql("UPDATE barang.tax_rules SET name = code WHERE name = '';");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_engine",
                schema: "barang",
                table: "tax_rules",
                columns: new[] { "company_id", "currency_code", "component_type", "product_id", "product_variant_id", "valid_from" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_rules_engine",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "component_type",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "customer_tax_status_code",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "product_id",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropColumn(
                name: "service_code",
                schema: "barang",
                table: "tax_rules");
        }
    }
}
