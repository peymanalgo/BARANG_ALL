using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_referral_codes",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_referral_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_referrals",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referrer_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referred_customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_used = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reward_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reward_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rewarded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_referrals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_referral_codes_company_id",
                schema: "barang",
                table: "customer_referral_codes",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_customer_referral_codes_code",
                schema: "barang",
                table: "customer_referral_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customer_referral_codes_customer",
                schema: "barang",
                table: "customer_referral_codes",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_referrals_company_id",
                schema: "barang",
                table: "customer_referrals",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_referrals_referrer_created",
                schema: "barang",
                table: "customer_referrals",
                columns: new[] { "referrer_customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_referrals_referred",
                schema: "barang",
                table: "customer_referrals",
                column: "referred_customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_referral_codes",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customer_referrals",
                schema: "barang");
        }
    }
}
