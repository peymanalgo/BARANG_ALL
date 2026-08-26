using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861 // EF migration column arrays are generated migration metadata.

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditAndObservabilityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    action = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    request_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    failure_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    changes = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.audit_events
                    (id, occurred_at_utc, company_id, actor_user_id, actor_username,
                     actor_type, action, entity_type, entity_id, correlation_id,
                     request_id, source, outcome, failure_code, ip_address,
                     user_agent, metadata, changes)
                SELECT id, occurred_at_utc, company_id, actor_user_id, NULL,
                       CASE WHEN actor_user_id IS NULL THEN 'Anonymous' ELSE 'InternalUser' END,
                       event_code, target_type, target_id::text,
                       COALESCE(correlation_id, 'legacy'), NULL, source_channel,
                       CASE WHEN event_code LIKE '%.failed' THEN 'Failure' ELSE 'Success' END,
                       NULL, NULL, NULL, '{}'::jsonb, '{}'::jsonb
                FROM barang.security_audit_events;
                """);

            migrationBuilder.DropTable(
                name: "security_audit_events",
                schema: "barang");

            migrationBuilder.InsertData(
                schema: "barang",
                table: "permissions",
                columns: new[] { "id", "code", "module", "technical_description" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000008"), "audit.read", "audit", "audit.read" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_action_occurred_at_utc",
                schema: "barang",
                table: "audit_events",
                columns: new[] { "action", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_user_id_occurred_at_utc",
                schema: "barang",
                table: "audit_events",
                columns: new[] { "actor_user_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_company_id_occurred_at_utc",
                schema: "barang",
                table: "audit_events",
                columns: new[] { "company_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation_id",
                schema: "barang",
                table: "audit_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity_occurred_at_utc",
                schema: "barang",
                table: "audit_events",
                columns: new[] { "entity_type", "entity_id", "occurred_at_utc" });

            migrationBuilder.Sql("""
                ALTER TABLE barang.audit_events ENABLE ROW LEVEL SECURITY;

                CREATE POLICY audit_events_company_read
                ON barang.audit_events FOR SELECT
                USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);

                CREATE POLICY audit_events_append
                ON barang.audit_events FOR INSERT
                WITH CHECK (
                    company_id IS NULL
                    OR company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid
                    OR EXISTS (
                        SELECT 1 FROM barang.user_companies access
                        WHERE access.user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid
                          AND access.company_id = audit_events.company_id
                          AND access.valid_from_utc <= now()
                          AND (access.valid_to_utc IS NULL OR access.valid_to_utc > now())
                    )
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP POLICY IF EXISTS audit_events_append ON barang.audit_events;
                DROP POLICY IF EXISTS audit_events_company_read ON barang.audit_events;
                ALTER TABLE barang.audit_events DISABLE ROW LEVEL SECURITY;
                """);

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"));

            migrationBuilder.CreateTable(
                name: "security_audit_events",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    event_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_security_audit_events", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO barang.security_audit_events
                    (id, event_code, actor_user_id, target_type, target_id,
                     company_id, occurred_at_utc, correlation_id, source_channel)
                SELECT id, action, actor_user_id, COALESCE(entity_type, 'unknown'),
                       CASE WHEN entity_id ~ '^[0-9a-fA-F-]{36}$' THEN entity_id::uuid ELSE NULL END,
                       company_id, occurred_at_utc, correlation_id, source
                FROM barang.audit_events;
                """);

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "barang");

            migrationBuilder.CreateIndex(
                name: "ix_security_audit_events_occurred_at_utc",
                schema: "barang",
                table: "security_audit_events",
                column: "occurred_at_utc");
        }
    }
}
#pragma warning restore CA1861
