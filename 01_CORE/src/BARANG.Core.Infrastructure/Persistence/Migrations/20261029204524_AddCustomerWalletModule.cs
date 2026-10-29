using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerWalletModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_wallet_topups",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    gateway_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_token = table.Column<Guid>(type: "uuid", nullable: false),
                    gateway_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_wallet_topups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_wallet_topups_company_id",
                schema: "barang",
                table: "customer_wallet_topups",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wallet_topups_customer_created",
                schema: "barang",
                table: "customer_wallet_topups",
                columns: new[] { "customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "uq_customer_wallet_topups_number",
                schema: "barang",
                table: "customer_wallet_topups",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_customer_wallet_topups_payment_token",
                schema: "barang",
                table: "customer_wallet_topups",
                column: "payment_token",
                unique: true);
            migrationBuilder.Sql(@"CREATE SEQUENCE IF NOT EXISTS barang.wallet_topup_seq START WITH 1 INCREMENT BY 1;");
            migrationBuilder.Sql(@"DO $seed$
                DECLARE
                    cid uuid;
                BEGIN
                    FOR cid IN SELECT id FROM barang.companies LOOP
                        PERFORM set_config('barang.company_id', cid::text, false);
                        INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                        SELECT gen_random_uuid(),t.id,cid,'topup',15,true,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                        FROM barang.lookup_types t WHERE t.code='wallet.reason' AND t.company_id=cid
                        AND NOT EXISTS(SELECT 1 FROM barang.lookup_values x WHERE x.lookup_type_id=t.id AND x.code='topup');
                        INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,created_at_utc,updated_at_utc,version)
                        SELECT gen_random_uuid(),v.id,cid,c.culture,c.name,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                        FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id AND t.code='wallet.reason' AND t.company_id=cid
                        CROSS JOIN (VALUES ('fa','شارژ کیف پول'),('en','Wallet top-up'),('ar','شحن المحفظة')) AS c(culture,name)
                        WHERE v.code='topup'
                        AND NOT EXISTS(SELECT 1 FROM barang.lookup_value_translations x WHERE x.lookup_value_id=v.id AND x.culture=c.culture);
                        -- payment.method 'wallet' is a single GLOBAL value (unique type+code).
                        -- Global writes need a system admin (RLS); skipped gracefully before the first admin exists.
                        BEGIN
                            PERFORM set_config('barang.user_id', COALESCE((SELECT u.id::text FROM barang.application_users u JOIN barang.user_roles a ON a.user_id=u.id JOIN barang.roles r ON r.id=a.role_id WHERE r.code='system.administrator' AND u.account_state='Active' LIMIT 1), ''), false);
                            INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                            SELECT gen_random_uuid(),t.id,NULL,'wallet',40,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                            FROM barang.lookup_types t WHERE t.code='payment.method' AND t.company_id IS NULL
                            AND NOT EXISTS(SELECT 1 FROM barang.lookup_values x WHERE x.lookup_type_id=t.id AND x.code='wallet');
                            INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,created_at_utc,updated_at_utc,version)
                            SELECT gen_random_uuid(),v.id,NULL,c.culture,c.name,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                            FROM barang.lookup_values v JOIN barang.lookup_types t ON t.id=v.lookup_type_id AND t.code='payment.method'
                            CROSS JOIN (VALUES ('fa','کیف پول'),('en','Wallet'),('ar','المحفظة')) AS c(culture,name)
                            WHERE v.code='wallet' AND v.company_id IS NULL
                            AND NOT EXISTS(SELECT 1 FROM barang.lookup_value_translations x WHERE x.lookup_value_id=v.id AND x.culture=c.culture);
                        EXCEPTION WHEN OTHERS THEN NULL; END;                    END LOOP;
                    PERFORM set_config('barang.company_id', '', false);
                END
                $seed$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_wallet_topups",
                schema: "barang");
            migrationBuilder.Sql(@"DROP SEQUENCE IF EXISTS barang.wallet_topup_seq;");
        }
    }
}
