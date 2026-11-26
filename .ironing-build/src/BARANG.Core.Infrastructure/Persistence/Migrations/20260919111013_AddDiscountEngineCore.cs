using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountEngineCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "application_scope",
                schema: "barang",
                table: "discount_rules",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Order");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_group_id",
                schema: "barang",
                table: "discount_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "barang",
                table: "discount_rules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "discount_source",
                schema: "barang",
                table: "discount_rules",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Automatic");

            migrationBuilder.AddColumn<bool>(
                name: "is_combinable",
                schema: "barang",
                table: "discount_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_amount",
                schema: "barang",
                table: "discount_rules",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_quantity",
                schema: "barang",
                table: "discount_rules",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "barang",
                table: "discount_rules",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "priority",
                schema: "barang",
                table: "discount_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                schema: "barang",
                table: "discount_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_variant_id",
                schema: "barang",
                table: "discount_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reason_required",
                schema: "barang",
                table: "discount_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_discount_rules_engine",
                schema: "barang",
                table: "discount_rules",
                columns: new[] { "company_id", "application_scope", "priority", "valid_from" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_discount_rules_thresholds",
                schema: "barang",
                table: "discount_rules",
                sql: "minimum_quantity IS NULL OR minimum_quantity >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_discount_rules_engine",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropCheckConstraint(
                name: "ck_discount_rules_thresholds",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "application_scope",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "customer_group_id",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "discount_source",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "is_combinable",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "minimum_amount",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "minimum_quantity",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "priority",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "product_id",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "product_variant_id",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "reason_required",
                schema: "barang",
                table: "discount_rules");
        }
    }
}
