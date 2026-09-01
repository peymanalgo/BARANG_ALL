using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF migration-generated constant arrays

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundJobsNotificationsAndCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    template_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.UniqueConstraint("AK_notifications_company_id_id", x => new { x.company_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dead_letter_items",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outbox_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letter_items", x => x.id);
                    table.UniqueConstraint("AK_dead_letter_items_company_id_id", x => new { x.company_id, x.id });
                    table.ForeignKey(
                        name: "FK_dead_letter_items_outbox_messages_outbox_message_id",
                        column: x => x.outbox_message_id,
                        principalSchema: "barang",
                        principalTable: "outbox_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000060"), "notification.read", "notification", "notification.read" },
                    { new Guid("00000000-0000-0000-0000-000000000061"), "notification.manage", "notification", "notification.manage" },
                    { new Guid("00000000-0000-0000-0000-000000000062"), "backgroundjob.review", "backgroundjob", "backgroundjob.review" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_items_company_id_outbox_message_id",
                schema: "barang",
                table: "dead_letter_items",
                columns: new[] { "company_id", "outbox_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_items_company_id_status_created_at_utc",
                schema: "barang",
                table: "dead_letter_items",
                columns: new[] { "company_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dead_letter_items_outbox_message_id",
                schema: "barang",
                table: "dead_letter_items",
                column: "outbox_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_company_id_recipient_user_id_read_at_utc_crea~",
                schema: "barang",
                table: "notifications",
                columns: new[] { "company_id", "recipient_user_id", "read_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_company_id_status_scheduled_at_utc",
                schema: "barang",
                table: "notifications",
                columns: new[] { "company_id", "status", "scheduled_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_company_id_correlation_id_event_type",
                schema: "barang",
                table: "outbox_messages",
                columns: new[] { "company_id", "correlation_id", "event_type" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_status_next_attempt_at_utc_occurred_at_utc",
                schema: "barang",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at_utc", "occurred_at_utc" });
            migrationBuilder.Sql("""
                CREATE SCHEMA IF NOT EXISTS hangfire;
                INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
                SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code='system.administrator' AND p.code IN ('notification.read','notification.manage','backgroundjob.review') ON CONFLICT(role_id,permission_id) DO NOTHING;
                ALTER TABLE barang.outbox_messages ENABLE ROW LEVEL SECURITY; CREATE POLICY outbox_messages_company_isolation ON barang.outbox_messages USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.notifications ENABLE ROW LEVEL SECURITY; CREATE POLICY notifications_company_isolation ON barang.notifications USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                ALTER TABLE barang.dead_letter_items ENABLE ROW LEVEL SECURITY; CREATE POLICY dead_letter_items_company_isolation ON barang.dead_letter_items USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT USAGE,CREATE ON SCHEMA hangfire TO barang_app; GRANT SELECT,INSERT,UPDATE ON barang.outbox_messages,barang.notifications,barang.dead_letter_items TO barang_app; REVOKE DELETE ON barang.outbox_messages,barang.notifications,barang.dead_letter_items FROM barang_app; END IF;
                 IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN GRANT USAGE,CREATE ON SCHEMA hangfire TO barang_test_app; GRANT SELECT,INSERT,UPDATE ON barang.outbox_messages,barang.notifications,barang.dead_letter_items TO barang_test_app; REVOKE DELETE ON barang.outbox_messages,barang.notifications,barang.dead_letter_items FROM barang_test_app; END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000060','00000000-0000-0000-0000-000000000061','00000000-0000-0000-0000-000000000062');");
            migrationBuilder.DropTable(
                name: "dead_letter_items",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "barang");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "barang");

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000060"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000061"));

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000062"));
        }
    }
}
