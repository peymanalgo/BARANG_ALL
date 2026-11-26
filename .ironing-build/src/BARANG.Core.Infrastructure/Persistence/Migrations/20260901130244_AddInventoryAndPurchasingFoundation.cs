using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration arrays are one-shot schema metadata.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndPurchasingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_warehouses_company_id_id",
                schema: "barang",
                table: "warehouses",
                columns: new[] { "company_id", "id" });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    currency_code = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    closure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.id);
                    table.UniqueConstraint("AK_purchase_orders_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_company_id_supplier_id",
                        columns: x => new { x.company_id, x.supplier_id },
                        principalSchema: "barang",
                        principalTable: "suppliers",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requests",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_shortage_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_requests", x => x.id);
                    table.UniqueConstraint("AK_purchase_requests_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_purchase_requests_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchasing_history",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchasing_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balances", x => x.id);
                    table.CheckConstraint("ck_stock_balances_on_hand", "on_hand >= 0");
                    table.CheckConstraint("ck_stock_balances_reserved", "reserved >= 0 AND reserved <= on_hand");
                    table.ForeignKey(
                        name: "FK_stock_balances_product_variants_company_id_product_variant_~",
                        columns: x => new { x.company_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    remaining_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                    table.CheckConstraint("ck_stock_reservations_qty", "requested_quantity > 0 AND reserved_quantity > 0 AND remaining_quantity >= 0 AND remaining_quantity <= reserved_quantity");
                    table.ForeignKey(
                        name: "FK_stock_reservations_order_items_company_id_order_item_id",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_product_variants_company_id_product_vari~",
                        columns: x => new { x.company_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_shortages",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    required_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    available_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    shortage_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_shortages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_transactions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transactions", x => x.id);
                    table.CheckConstraint("ck_stock_transactions_quantity", "quantity_delta <> 0");
                    table.ForeignKey(
                        name: "FK_stock_transactions_product_variants_company_id_product_vari~",
                        columns: x => new { x.company_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transactions_warehouses_company_id_warehouse_id",
                        columns: x => new { x.company_id, x.warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transfers", x => x.id);
                    table.CheckConstraint("ck_stock_transfers_qty", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_stock_transfers_product_variants_company_id_product_variant~",
                        columns: x => new { x.company_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_company_id_destination_warehouse~",
                        columns: x => new { x.company_id, x.destination_warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_company_id_source_warehouse_id",
                        columns: x => new { x.company_id, x.source_warehouse_id },
                        principalSchema: "barang",
                        principalTable: "warehouses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_items", x => x.id);
                    table.UniqueConstraint("AK_purchase_order_items_company_id_id", x => new { x.company_id, x.id });
                    table.CheckConstraint("ck_purchase_order_items_values", "ordered_quantity > 0 AND unit_purchase_price >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_orders_company_id_purchase_or~",
                        columns: x => new { x.company_id, x.purchase_order_id },
                        principalSchema: "barang",
                        principalTable: "purchase_orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_request_items",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_request_items", x => x.id);
                    table.CheckConstraint("ck_purchase_request_items_qty", "requested_quantity > 0");
                    table.ForeignKey(
                        name: "FK_purchase_request_items_purchase_requests_company_id_purchas~",
                        columns: x => new { x.company_id, x.purchase_request_id },
                        principalSchema: "barang",
                        principalTable: "purchase_requests",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipts", x => x.id);
                    table.CheckConstraint("ck_purchase_receipts_qty", "quantity > 0");
                    table.ForeignKey(
                        name: "FK_purchase_receipts_purchase_order_items_company_id_purchase_~",
                        columns: x => new { x.company_id, x.purchase_order_item_id },
                        principalSchema: "barang",
                        principalTable: "purchase_order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000038"), "inventory.read", "inventory", "inventory.read" },
                    { new Guid("00000000-0000-0000-0000-000000000039"), "inventory.manage", "inventory", "inventory.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000040"), "inventory.adjust", "inventory", "inventory.adjust" },
                    { new Guid("00000000-0000-0000-0000-000000000041"), "purchasing.read", "purchasing", "purchasing.read" },
                    { new Guid("00000000-0000-0000-0000-000000000042"), "purchasing.manage", "purchasing", "purchasing.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_company_id_purchase_order_id",
                schema: "barang",
                table: "purchase_order_items",
                columns: new[] { "company_id", "purchase_order_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_company_id_purchase_order_number",
                schema: "barang",
                table: "purchase_orders",
                columns: new[] { "company_id", "purchase_order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_company_id_status_created_at_utc",
                schema: "barang",
                table: "purchase_orders",
                columns: new[] { "company_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_company_id_supplier_id",
                schema: "barang",
                table: "purchase_orders",
                columns: new[] { "company_id", "supplier_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_company_id_warehouse_id",
                schema: "barang",
                table: "purchase_orders",
                columns: new[] { "company_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_company_id_purchase_order_item_id",
                schema: "barang",
                table: "purchase_receipts",
                columns: new[] { "company_id", "purchase_order_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_company_id_reference",
                schema: "barang",
                table: "purchase_receipts",
                columns: new[] { "company_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_request_items_company_id_purchase_request_id",
                schema: "barang",
                table: "purchase_request_items",
                columns: new[] { "company_id", "purchase_request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_requests_company_id_warehouse_id",
                schema: "barang",
                table: "purchase_requests",
                columns: new[] { "company_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_purchasing_history_company_id_entity_type_entity_id_occurre~",
                schema: "barang",
                table: "purchasing_history",
                columns: new[] { "company_id", "entity_type", "entity_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_company_id_product_variant_id",
                schema: "barang",
                table: "stock_balances",
                columns: new[] { "company_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_company_id_warehouse_id_product_variant_id",
                schema: "barang",
                table: "stock_balances",
                columns: new[] { "company_id", "warehouse_id", "product_variant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_company_id_idempotency_key",
                schema: "barang",
                table: "stock_reservations",
                columns: new[] { "company_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_company_id_order_item_id",
                schema: "barang",
                table: "stock_reservations",
                columns: new[] { "company_id", "order_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_company_id_product_variant_id",
                schema: "barang",
                table: "stock_reservations",
                columns: new[] { "company_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_company_id_status_expires_at_utc",
                schema: "barang",
                table: "stock_reservations",
                columns: new[] { "company_id", "status", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_company_id_warehouse_id",
                schema: "barang",
                table: "stock_reservations",
                columns: new[] { "company_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_shortages_company_id_order_item_id_warehouse_id",
                schema: "barang",
                table: "stock_shortages",
                columns: new[] { "company_id", "order_item_id", "warehouse_id" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_company_id_product_variant_id",
                schema: "barang",
                table: "stock_transactions",
                columns: new[] { "company_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_company_id_reference",
                schema: "barang",
                table: "stock_transactions",
                columns: new[] { "company_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_company_id_warehouse_id_product_variant_~",
                schema: "barang",
                table: "stock_transactions",
                columns: new[] { "company_id", "warehouse_id", "product_variant_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_company_id_destination_warehouse_id",
                schema: "barang",
                table: "stock_transfers",
                columns: new[] { "company_id", "destination_warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_company_id_product_variant_id",
                schema: "barang",
                table: "stock_transfers",
                columns: new[] { "company_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_company_id_reference",
                schema: "barang",
                table: "stock_transfers",
                columns: new[] { "company_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_transfers_company_id_source_warehouse_id",
                schema: "barang",
                table: "stock_transfers",
                columns: new[] { "company_id", "source_warehouse_id" });

            migrationBuilder.Sql("""
                CREATE SEQUENCE barang.purchase_order_number_seq AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('inventory.read','inventory.manage','inventory.adjust','purchasing.read','purchasing.manage')
                ON CONFLICT(role_id,permission_id) DO NOTHING;
                DO $rls$ DECLARE t text; BEGIN
                  FOREACH t IN ARRAY ARRAY['stock_transactions','stock_balances','stock_reservations','stock_transfers','stock_shortages','purchase_requests','purchase_request_items','purchase_orders','purchase_order_items','purchase_receipts','purchasing_history'] LOOP
                    EXECUTE format('ALTER TABLE barang.%I ENABLE ROW LEVEL SECURITY',t);
                    EXECUTE format('CREATE POLICY %I ON barang.%I USING(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid) WITH CHECK(company_id=NULLIF(current_setting(''barang.company_id'',true),'''')::uuid)',t||'_company_isolation',t);
                  END LOOP;
                END $rls$;
                DO $grant$ BEGIN
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
                    GRANT SELECT,INSERT ON barang.stock_transactions,barang.purchase_receipts,barang.purchasing_history TO barang_app;
                    GRANT SELECT,INSERT,UPDATE ON barang.stock_balances,barang.stock_reservations,barang.stock_transfers,barang.stock_shortages,barang.purchase_requests,barang.purchase_request_items,barang.purchase_orders,barang.purchase_order_items TO barang_app;
                    GRANT USAGE,SELECT ON SEQUENCE barang.purchase_order_number_seq TO barang_app;
                    REVOKE UPDATE,DELETE ON barang.stock_transactions,barang.purchase_receipts,barang.purchasing_history FROM barang_app;
                    REVOKE DELETE ON barang.stock_balances,barang.stock_reservations,barang.stock_transfers,barang.stock_shortages,barang.purchase_requests,barang.purchase_request_items,barang.purchase_orders,barang.purchase_order_items FROM barang_app;
                  END IF;
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN
                    GRANT SELECT,INSERT ON barang.stock_transactions,barang.purchase_receipts,barang.purchasing_history TO barang_test_app;
                    GRANT SELECT,INSERT,UPDATE ON barang.stock_balances,barang.stock_reservations,barang.stock_transfers,barang.stock_shortages,barang.purchase_requests,barang.purchase_request_items,barang.purchase_orders,barang.purchase_order_items TO barang_test_app;
                    GRANT USAGE,SELECT ON SEQUENCE barang.purchase_order_number_seq TO barang_test_app;
                    REVOKE UPDATE,DELETE ON barang.stock_transactions,barang.purchase_receipts,barang.purchasing_history FROM barang_test_app;
                    REVOKE DELETE ON barang.stock_balances,barang.stock_reservations,barang.stock_transfers,barang.stock_shortages,barang.purchase_requests,barang.purchase_request_items,barang.purchase_orders,barang.purchase_order_items FROM barang_test_app;
                  END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000038','00000000-0000-0000-0000-000000000039','00000000-0000-0000-0000-000000000040','00000000-0000-0000-0000-000000000041','00000000-0000-0000-0000-000000000042'); DROP SEQUENCE IF EXISTS barang.purchase_order_number_seq;");
            migrationBuilder.DropTable(
                name: "purchase_receipts",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "purchase_request_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "purchasing_history",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stock_reservations",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stock_shortages",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stock_transactions",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "stock_transfers",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "purchase_order_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "purchase_requests",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "barang");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_warehouses_company_id_id",
                schema: "barang",
                table: "warehouses");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000040"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000042"));
        }
    }
}
