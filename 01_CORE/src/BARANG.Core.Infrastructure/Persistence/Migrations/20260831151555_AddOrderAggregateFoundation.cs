using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration array literals.

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAggregateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_customer_addresses_company_id_id",
                schema: "barang",
                table: "customer_addresses",
                columns: new[] { "company_id", "id" });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    requested_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cancellation_reason_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    shipping_address_snapshot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.UniqueConstraint("ak_orders_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_orders_application_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "barang",
                        principalTable: "application_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_branches_company_id_branch_id",
                        columns: x => new { x.company_id, x.branch_id },
                        principalSchema: "barang",
                        principalTable: "branches",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_customers_customer_id_company_id",
                        columns: x => new { x.customer_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "customers",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_address_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_customer_address_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    province = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    city = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    district = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    address_line1 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    address_line2 = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_address_snapshots", x => x.id);
                    table.UniqueConstraint("ak_order_address_snapshots_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_address_snapshots_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_address_snapshots_customer_addresses_company_id_sourc~",
                        columns: x => new { x.company_id, x.source_customer_address_id },
                        principalSchema: "barang",
                        principalTable: "customer_addresses",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_address_snapshots_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_change_requests",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    change_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_changes_json = table.Column<string>(type: "jsonb", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_change_requests", x => x.id);
                    table.UniqueConstraint("ak_order_change_requests_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_change_requests_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_change_requests_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    snapshot_product_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    snapshot_product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    snapshot_variant_sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    snapshot_variant_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    snapshot_size_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    snapshot_color_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    snapshot_uom_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.UniqueConstraint("ak_order_items_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_items_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_product_variants_company_id_product_id_product_~",
                        columns: x => new { x.company_id, x.product_id, x.product_variant_id },
                        principalSchema: "barang",
                        principalTable: "product_variants",
                        principalColumns: new[] { "company_id", "product_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_items_products_company_id_product_id",
                        columns: x => new { x.company_id, x.product_id },
                        principalSchema: "barang",
                        principalTable: "products",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_timeline_events",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_timeline_events", x => x.id);
                    table.UniqueConstraint("ak_order_timeline_events_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_timeline_events_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_timeline_events_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_print_operations",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    print_position_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    print_method_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    width = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    height = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    artwork_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_print_operations", x => x.id);
                    table.UniqueConstraint("ak_order_print_operations_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_order_print_operations_artwork_versions_company_id_artwork_~",
                        columns: x => new { x.company_id, x.artwork_version_id },
                        principalSchema: "barang",
                        principalTable: "artwork_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_print_operations_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "barang",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_print_operations_file_versions_company_id_file_versio~",
                        columns: x => new { x.company_id, x.file_version_id },
                        principalSchema: "barang",
                        principalTable: "file_versions",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_print_operations_order_items_company_id_order_item_id",
                        columns: x => new { x.company_id, x.order_item_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_print_operations_orders_company_id_order_id",
                        columns: x => new { x.company_id, x.order_id },
                        principalSchema: "barang",
                        principalTable: "orders",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000024"), "order.read", "order", "order.read" },
                    { new Guid("00000000-0000-0000-0000-000000000025"), "order.create", "order", "order.create" },
                    { new Guid("00000000-0000-0000-0000-000000000026"), "order.manage", "order", "order.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000027"), "order.submit", "order", "order.submit" },
                    { new Guid("00000000-0000-0000-0000-000000000028"), "order.cancel", "order", "order.cancel" },
                    { new Guid("00000000-0000-0000-0000-000000000029"), "order.change.manage", "order", "order.change.manage" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_address_snapshots_company_id_order_id",
                schema: "barang",
                table: "order_address_snapshots",
                columns: new[] { "company_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_address_snapshots_company_id_source_customer_address_~",
                schema: "barang",
                table: "order_address_snapshots",
                columns: new[] { "company_id", "source_customer_address_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_change_requests_company_id_order_id_status",
                schema: "barang",
                table: "order_change_requests",
                columns: new[] { "company_id", "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_company_id_product_id_product_variant_id",
                schema: "barang",
                table: "order_items",
                columns: new[] { "company_id", "product_id", "product_variant_id" });

            migrationBuilder.CreateIndex(
                name: "uq_order_items_line",
                schema: "barang",
                table: "order_items",
                columns: new[] { "company_id", "order_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_print_operations_company_id_artwork_version_id",
                schema: "barang",
                table: "order_print_operations",
                columns: new[] { "company_id", "artwork_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_print_operations_company_id_file_version_id",
                schema: "barang",
                table: "order_print_operations",
                columns: new[] { "company_id", "file_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_order_print_operations_company_id_order_id",
                schema: "barang",
                table: "order_print_operations",
                columns: new[] { "company_id", "order_id" });

            migrationBuilder.CreateIndex(
                name: "uq_order_print_sequence",
                schema: "barang",
                table: "order_print_operations",
                columns: new[] { "company_id", "order_item_id", "sequence" },
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_order_timeline_events_company_id_order_id_occurred_at_utc_id",
                schema: "barang",
                table: "order_timeline_events",
                columns: new[] { "company_id", "order_id", "occurred_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_branch",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by_user_id",
                schema: "barang",
                table: "orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "customer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id_company_id",
                schema: "barang",
                table: "orders",
                columns: new[] { "customer_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "uq_orders_company_number",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "order_number" },
                unique: true,
                filter: "order_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_orders_external_reference",
                schema: "barang",
                table: "orders",
                columns: new[] { "company_id", "source", "external_reference" },
                unique: true,
                filter: "external_reference IS NOT NULL");

            migrationBuilder.Sql("""
                CREATE SEQUENCE barang.order_number_seq AS bigint START WITH 1 INCREMENT BY 1 NO CYCLE;
                ALTER TABLE barang.orders ADD CONSTRAINT ck_orders_version_positive CHECK (version > 0);
                ALTER TABLE barang.order_items ADD CONSTRAINT ck_order_items_version_quantity CHECK (version > 0 AND quantity > 0);
                ALTER TABLE barang.order_print_operations ADD CONSTRAINT ck_order_print_operations_version_dimensions CHECK (version > 0 AND (width IS NULL OR width > 0) AND (height IS NULL OR height > 0));
                ALTER TABLE barang.order_address_snapshots ADD CONSTRAINT ck_order_address_snapshots_version_positive CHECK (version > 0);
                ALTER TABLE barang.order_change_requests ADD CONSTRAINT ck_order_change_requests_version_positive CHECK (version > 0);

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.code IN ('order.read','order.create','order.manage','order.submit','order.cancel','order.change.manage')
                ON CONFLICT (role_id, permission_id) DO NOTHING;

                ALTER TABLE barang.orders ENABLE ROW LEVEL SECURITY;
                CREATE POLICY orders_company_isolation ON barang.orders USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_items ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_items_company_isolation ON barang.order_items USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_print_operations ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_print_operations_company_isolation ON barang.order_print_operations USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_address_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_address_snapshots_company_isolation ON barang.order_address_snapshots USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_change_requests ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_change_requests_company_isolation ON barang.order_change_requests USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                ALTER TABLE barang.order_timeline_events ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_timeline_events_company_isolation ON barang.order_timeline_events USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);
                DO $grant$
                BEGIN
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.orders, barang.order_items, barang.order_print_operations, barang.order_address_snapshots, barang.order_change_requests, barang.order_timeline_events TO barang_app;
                    GRANT USAGE, SELECT ON SEQUENCE barang.order_number_seq TO barang_app;
                    REVOKE DELETE ON barang.orders, barang.order_items, barang.order_print_operations, barang.order_address_snapshots, barang.order_change_requests, barang.order_timeline_events FROM barang_app;
                  END IF;
                  IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'barang_test_app') THEN
                    GRANT SELECT, INSERT, UPDATE ON barang.orders, barang.order_items, barang.order_print_operations, barang.order_address_snapshots, barang.order_change_requests, barang.order_timeline_events TO barang_test_app;
                    GRANT USAGE, SELECT ON SEQUENCE barang.order_number_seq TO barang_test_app;
                    REVOKE DELETE ON barang.orders, barang.order_items, barang.order_print_operations, barang.order_address_snapshots, barang.order_change_requests, barang.order_timeline_events FROM barang_test_app;
                  END IF;
                END
                $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000024','00000000-0000-0000-0000-000000000025','00000000-0000-0000-0000-000000000026','00000000-0000-0000-0000-000000000027','00000000-0000-0000-0000-000000000028','00000000-0000-0000-0000-000000000029'); DROP SEQUENCE IF EXISTS barang.order_number_seq;");
            migrationBuilder.DropTable(
                name: "order_address_snapshots",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_change_requests",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_print_operations",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_timeline_events",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "barang");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_customer_addresses_company_id_id",
                schema: "barang",
                table: "customer_addresses");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000029"));
        }
    }
}
