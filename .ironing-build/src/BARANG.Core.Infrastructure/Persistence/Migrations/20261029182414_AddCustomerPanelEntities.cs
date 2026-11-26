using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861
namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerPanelEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_custom_designs",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_custom_designs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_feedbacks",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_type_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    subject = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_feedbacks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customer_wallet_transactions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    reference_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_wallet_transactions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_custom_designs_company_id",
                schema: "barang",
                table: "customer_custom_designs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_custom_designs_customer_created",
                schema: "barang",
                table: "customer_custom_designs",
                columns: new[] { "customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_feedbacks_company_id",
                schema: "barang",
                table: "customer_feedbacks",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_feedbacks_customer_created",
                schema: "barang",
                table: "customer_feedbacks",
                columns: new[] { "customer_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_wallet_transactions_company_id",
                schema: "barang",
                table: "customer_wallet_transactions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_wallet_transactions_customer_created",
                schema: "barang",
                table: "customer_wallet_transactions",
                columns: new[] { "customer_id", "created_at_utc" });

            SeedCustomerPanelLookups(migrationBuilder);
        }

        private static void SeedCustomerPanelLookups(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $seed$
                DECLARE
                    cid uuid;
                BEGIN
                    FOR cid IN SELECT id FROM barang.companies LOOP
                        PERFORM set_config('barang.company_id', cid::text, false);

                        INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
                        SELECT gen_random_uuid(),cid,d.code,d.name,'Company',false,true,true,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                        FROM (VALUES ('wallet.reason','Wallet Transaction Reasons'),('feedback.type','Feedback Types'),('feedback.status','Feedback Statuses')) AS d(code,name)
                        WHERE NOT EXISTS(SELECT 1 FROM barang.lookup_types t WHERE t.code=d.code AND t.company_id=cid);

                        INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                        SELECT gen_random_uuid(),t.id,cid,v.code,v.display_order,true,false,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                        FROM (VALUES
                            ('wallet.reason','manual_adjustment',10),
                            ('wallet.reason','purchase',20),
                            ('wallet.reason','order_refund',30),
                            ('wallet.reason','gift',40),
                            ('feedback.type','suggestion',10),
                            ('feedback.type','complaint',20),
                            ('feedback.type','review',30),
                            ('feedback.type','other',40),
                            ('feedback.status','new',10),
                            ('feedback.status','in_review',20),
                            ('feedback.status','resolved',30)
                        ) AS v(type_code,code,display_order)
                        JOIN barang.lookup_types t ON t.code=v.type_code AND t.company_id=cid
                        WHERE NOT EXISTS(SELECT 1 FROM barang.lookup_values x WHERE x.lookup_type_id=t.id AND x.code=v.code);

                        INSERT INTO barang.lookup_value_translations(id,lookup_value_id,company_id,culture,display_name,description,created_at_utc,updated_at_utc,version)
                        SELECT gen_random_uuid(),lv.id,cid,cul.culture,
                               CASE cul.culture WHEN 'fa' THEN c.fa WHEN 'en' THEN c.en ELSE c.ar END,
                               NULL,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1
                        FROM (VALUES
                            ('manual_adjustment','تنظیم دستی','Manual Adjustment','تعديل يدوي'),
                            ('purchase','خرید','Purchase','شراء'),
                            ('order_refund','بازگشت وجه سفارش','Order Refund','استرداد الطلب'),
                            ('gift','هدیه','Gift','هدية'),
                            ('suggestion','پیشنهاد','Suggestion','اقتراح'),
                            ('complaint','انتقاد','Complaint','شكوى'),
                            ('review','دیدگاه','Review','مراجعة'),
                            ('other','سایر','Other','أخرى'),
                            ('new','جدید','New','جديد'),
                            ('in_review','در حال بررسی','In Review','قيد المراجعة'),
                            ('resolved','حل شده','Resolved','تم الحل')
                        ) AS c(code,fa,en,ar)
                        JOIN barang.lookup_values lv ON lv.code=c.code
                        JOIN barang.lookup_types t ON t.id=lv.lookup_type_id AND t.code IN ('wallet.reason','feedback.type','feedback.status') AND t.company_id=cid
                        CROSS JOIN (VALUES ('fa'),('en'),('ar')) AS cul(culture)
                        WHERE NOT EXISTS(SELECT 1 FROM barang.lookup_value_translations x WHERE x.lookup_value_id=lv.id AND x.culture=cul.culture);

                    END LOOP;
                    PERFORM set_config('barang.company_id', '', false);
                END
                $seed$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_custom_designs",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customer_feedbacks",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "customer_wallet_transactions",
                schema: "barang");
        }
    }
}
