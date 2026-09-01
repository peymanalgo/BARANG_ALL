using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsFinanceAndInvoiceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "finance_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_finance_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_finance_history_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    invoice_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.UniqueConstraint("AK_invoices_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_invoices_order_price_snapshots_company_id_price_snapshot_id",
                        columns: x => new { x.company_id, x.price_snapshot_id },
                        principalSchema: "barang",
                        principalTable: "order_price_snapshots",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_financial_states",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_financial_states", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_financial_states_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payment_method_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    paid_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    registered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    original_payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.UniqueConstraint("AK_payments_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_payments_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_payments_company_id_original_payment_id",
                        columns: x => new { x.company_id, x.original_payment_id },
                        principalSchema: "barang",
                        principalTable: "payments",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_item_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    variant_sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    final_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_item_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_item_snapshots_invoices_company_id_invoice_id",
                        columns: x => new { x.company_id, x.invoice_id },
                        principalSchema: "barang",
                        principalTable: "invoices",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    customer_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    raw_product_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    print_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    additional_service_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    packaging_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    shipping_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    final_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_snapshots_invoices_company_id_invoice_id",
                        columns: x => new { x.company_id, x.invoice_id },
                        principalSchema: "barang",
                        principalTable: "invoices",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000033"), "finance.read", "finance", "finance.read" },
                    { new Guid("00000000-0000-0000-0000-000000000034"), "payment.manage", "payment", "payment.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000035"), "payment.reverse", "payment", "payment.reverse" },
                    { new Guid("00000000-0000-0000-0000-000000000036"), "finance.approve", "finance", "finance.approve" },
                    { new Guid("00000000-0000-0000-0000-000000000037"), "invoice.manage", "invoice", "invoice.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_finance_history_company_id_order_id_occurred_at_utc_id",
                schema: "barang",
                table: "finance_history",
                columns: new[] { "company_id", "order_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_snapshots_company_id_invoice_id_line_number",
                schema: "barang",
                table: "invoice_item_snapshots",
                columns: new[] { "company_id", "invoice_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_snapshots_company_id_invoice_id",
                schema: "barang",
                table: "invoice_snapshots",
                columns: new[] { "company_id", "invoice_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_company_id_invoice_number",
                schema: "barang",
                table: "invoices",
                columns: new[] { "company_id", "invoice_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_company_id_order_id_invoice_type",
                schema: "barang",
                table: "invoices",
                columns: new[] { "company_id", "order_id", "invoice_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_company_id_price_snapshot_id",
                schema: "barang",
                table: "invoices",
                columns: new[] { "company_id", "price_snapshot_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_financial_states_company_id_order_id",
                schema: "barang",
                table: "order_financial_states",
                columns: new[] { "company_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_company_id_order_id_paid_at_utc",
                schema: "barang",
                table: "payments",
                columns: new[] { "company_id", "order_id", "paid_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_company_id_original_payment_id",
                schema: "barang",
                table: "payments",
                columns: new[] { "company_id", "original_payment_id" });

            migrationBuilder.CreateIndex(
                name: "uq_payments_company_reference",
                schema: "barang",
                table: "payments",
                columns: new[] { "company_id", "reference_number" },
                unique: true,
                filter: "reference_number IS NOT NULL");

            migrationBuilder.Sql("""
                CREATE SEQUENCE barang.invoice_number_seq AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;
                ALTER TABLE barang.payments ADD CONSTRAINT ck_payments_record_shape CHECK
                  ((record_type = 'Payment' AND original_payment_id IS NULL AND reversal_reason IS NULL) OR
                   (record_type = 'Reversal' AND original_payment_id IS NOT NULL AND reversal_reason IS NOT NULL));
                ALTER TABLE barang.payments ADD CONSTRAINT ck_payments_version_positive CHECK (version > 0);
                ALTER TABLE barang.order_financial_states ADD CONSTRAINT ck_order_financial_states_version_positive CHECK (version > 0);
                ALTER TABLE barang.invoices ADD CONSTRAINT ck_invoices_version_positive CHECK (version > 0);

                INSERT INTO barang.lookup_types(id,company_id,code,display_name,scope,is_system,is_active,allow_custom_values,created_at_utc,updated_at_utc,version)
                VALUES('13000000-0000-0000-0000-000000000001',NULL,'payment.method','Payment Method','Global',TRUE,TRUE,FALSE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT(code) DO NOTHING;
                INSERT INTO barang.lookup_values(id,lookup_type_id,company_id,code,display_order,is_active,is_system,created_at_utc,updated_at_utc,version)
                VALUES
                  ('13000000-0000-0000-0000-000000000011','13000000-0000-0000-0000-000000000001',NULL,'cash',10,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                  ('13000000-0000-0000-0000-000000000012','13000000-0000-0000-0000-000000000001',NULL,'bank_transfer',20,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                  ('13000000-0000-0000-0000-000000000013','13000000-0000-0000-0000-000000000001',NULL,'card',30,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                  ('13000000-0000-0000-0000-000000000014','13000000-0000-0000-0000-000000000001',NULL,'online',40,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1),
                  ('13000000-0000-0000-0000-000000000015','13000000-0000-0000-0000-000000000001',NULL,'other',50,TRUE,TRUE,CURRENT_TIMESTAMP,CURRENT_TIMESTAMP,1)
                ON CONFLICT(lookup_type_id,code) DO NOTHING;

                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('finance.read','payment.manage','payment.reverse','finance.approve','invoice.manage')
                ON CONFLICT(role_id,permission_id) DO NOTHING;

                ALTER TABLE barang.payments ENABLE ROW LEVEL SECURITY;
                CREATE POLICY payments_company_isolation ON barang.payments USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.order_financial_states ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_financial_states_company_isolation ON barang.order_financial_states USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.invoices ENABLE ROW LEVEL SECURITY;
                CREATE POLICY invoices_company_isolation ON barang.invoices USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.invoice_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY invoice_snapshots_company_isolation ON barang.invoice_snapshots USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.invoice_item_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY invoice_item_snapshots_company_isolation ON barang.invoice_item_snapshots USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.finance_history ENABLE ROW LEVEL SECURITY;
                CREATE POLICY finance_history_company_isolation ON barang.finance_history USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
                    GRANT SELECT,INSERT ON barang.payments,barang.invoices,barang.invoice_snapshots,barang.invoice_item_snapshots,barang.finance_history TO barang_app;
                    GRANT SELECT,INSERT,UPDATE ON barang.order_financial_states TO barang_app;
                    GRANT USAGE,SELECT ON SEQUENCE barang.invoice_number_seq TO barang_app;
                    REVOKE DELETE ON barang.payments,barang.order_financial_states,barang.invoices,barang.invoice_snapshots,barang.invoice_item_snapshots,barang.finance_history FROM barang_app;
                  END IF;
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN
                    GRANT SELECT,INSERT ON barang.payments,barang.invoices,barang.invoice_snapshots,barang.invoice_item_snapshots,barang.finance_history TO barang_test_app;
                    GRANT SELECT,INSERT,UPDATE ON barang.order_financial_states TO barang_test_app;
                    GRANT USAGE,SELECT ON SEQUENCE barang.invoice_number_seq TO barang_test_app;
                    REVOKE DELETE ON barang.payments,barang.order_financial_states,barang.invoices,barang.invoice_snapshots,barang.invoice_item_snapshots,barang.finance_history FROM barang_test_app;
                  END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000033','00000000-0000-0000-0000-000000000034','00000000-0000-0000-0000-000000000035','00000000-0000-0000-0000-000000000036','00000000-0000-0000-0000-000000000037'); DELETE FROM barang.lookup_values WHERE lookup_type_id='13000000-0000-0000-0000-000000000001'; DELETE FROM barang.lookup_types WHERE id='13000000-0000-0000-0000-000000000001'; DROP SEQUENCE IF EXISTS barang.invoice_number_seq;");
            migrationBuilder.DropTable(
                name: "finance_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "invoice_item_snapshots",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "invoice_snapshots",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_financial_states",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000037"));
        }
    }
}
