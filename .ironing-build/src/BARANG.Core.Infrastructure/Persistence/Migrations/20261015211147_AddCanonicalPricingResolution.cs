using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalPricingResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_rules_resolution",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.AddColumn<Guid>(
                name: "price_group_id",
                schema: "barang",
                table: "tariffs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "pricing_rule_id",
                schema: "barang",
                table: "price_component_snapshots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "pricing_rule_version",
                schema: "barang",
                table: "price_component_snapshots",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_resolution",
                schema: "barang",
                table: "tax_rules",
                columns: new[] { "company_id", "code", "customer_tax_status_code", "valid_from" });

            migrationBuilder.CreateIndex(
                name: "IX_tariffs_price_group_id",
                schema: "barang",
                table: "tariffs",
                column: "price_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_discount_rules_customer_group_id",
                schema: "barang",
                table: "discount_rules",
                column: "customer_group_id");

            migrationBuilder.AddForeignKey(
                name: "FK_discount_rules_lookup_values_customer_group_id",
                schema: "barang",
                table: "discount_rules",
                column: "customer_group_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tariffs_lookup_values_price_group_id",
                schema: "barang",
                table: "tariffs",
                column: "price_group_id",
                principalSchema: "barang",
                principalTable: "lookup_values",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_discount_rules_lookup_values_customer_group_id",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_tariffs_lookup_values_price_group_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropIndex(
                name: "ix_tax_rules_resolution",
                schema: "barang",
                table: "tax_rules");

            migrationBuilder.DropIndex(
                name: "IX_tariffs_price_group_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropIndex(
                name: "IX_discount_rules_customer_group_id",
                schema: "barang",
                table: "discount_rules");

            migrationBuilder.DropColumn(
                name: "price_group_id",
                schema: "barang",
                table: "tariffs");

            migrationBuilder.DropColumn(
                name: "pricing_rule_id",
                schema: "barang",
                table: "price_component_snapshots");

            migrationBuilder.DropColumn(
                name: "pricing_rule_version",
                schema: "barang",
                table: "price_component_snapshots");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_resolution",
                schema: "barang",
                table: "tax_rules",
                columns: new[] { "company_id", "code", "valid_from" });
        }
    }
}
