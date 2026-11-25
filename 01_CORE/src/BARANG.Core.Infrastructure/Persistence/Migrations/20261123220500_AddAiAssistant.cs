using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// AI smart assistant schema: barang.ai_model_settings (singleton per company, API key
/// encrypted at rest), ai_conversations + ai_chat_messages (per-user history) and
/// ai_audit_log (admin visibility), plus the ai.settings.read/manage permission seeds
/// (stable catalog GUIDs 091/092) granted to the system.administrator role.
/// Idempotent SQL so it cannot conflict with sibling migrations generated in the same window.
/// </summary>
[Migration("20261123220500_AddAiAssistant")]
[DbContext(typeof(CoreDbContext))]
public partial class AddAiAssistant : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS barang.ai_model_settings (
                id uuid NOT NULL CONSTRAINT pk_ai_model_settings PRIMARY KEY,
                company_id uuid NOT NULL,
                provider_name character varying(100) NOT NULL DEFAULT '',
                base_url character varying(500) NOT NULL DEFAULT '',
                api_key_cipher text NULL,
                api_key_hint character varying(40) NULL,
                model_name character varying(200) NOT NULL DEFAULT '',
                temperature numeric(4,2) NOT NULL DEFAULT 0,
                max_output_tokens integer NOT NULL DEFAULT 0,
                timeout_seconds integer NOT NULL DEFAULT 0,
                system_prompt_override text NULL,
                tool_calling_enabled boolean NOT NULL DEFAULT false,
                is_enabled boolean NOT NULL DEFAULT false,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL,
                version bigint NOT NULL DEFAULT 0);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_ai_model_settings_company_id ON barang.ai_model_settings (company_id);

            CREATE TABLE IF NOT EXISTS barang.ai_conversations (
                id uuid NOT NULL CONSTRAINT pk_ai_conversations PRIMARY KEY,
                company_id uuid NOT NULL,
                user_id uuid NOT NULL,
                created_at_utc timestamp with time zone NOT NULL,
                updated_at_utc timestamp with time zone NOT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS uq_ai_conversations_company_id_user_id ON barang.ai_conversations (company_id, user_id);

            CREATE TABLE IF NOT EXISTS barang.ai_chat_messages (
                id uuid NOT NULL CONSTRAINT pk_ai_chat_messages PRIMARY KEY,
                conversation_id uuid NOT NULL,
                company_id uuid NOT NULL,
                user_id uuid NOT NULL,
                role character varying(20) NOT NULL DEFAULT 'user',
                content text NOT NULL DEFAULT '',
                tools_used text NULL,
                prompt_tokens integer NULL,
                completion_tokens integer NULL,
                culture character varying(10) NOT NULL DEFAULT 'fa',
                created_at_utc timestamp with time zone NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_ai_chat_messages_conversation_id ON barang.ai_chat_messages (conversation_id);
            CREATE INDEX IF NOT EXISTS ix_ai_chat_messages_company_user_created ON barang.ai_chat_messages (company_id, user_id, created_at_utc);

            CREATE TABLE IF NOT EXISTS barang.ai_audit_log (
                id uuid NOT NULL CONSTRAINT pk_ai_audit_log PRIMARY KEY,
                company_id uuid NOT NULL,
                actor_user_id uuid NULL,
                actor_username character varying(150) NOT NULL DEFAULT '',
                action character varying(100) NOT NULL DEFAULT '',
                detail text NOT NULL DEFAULT '',
                occurred_at_utc timestamp with time zone NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_ai_audit_log_company_occurred ON barang.ai_audit_log (company_id, occurred_at_utc);

            INSERT INTO barang.permissions (id, code, module, technical_description) VALUES
                ('00000000-0000-0000-0000-000000000091', 'ai.settings.read', 'ai', 'ai.settings.read'),
                ('00000000-0000-0000-0000-000000000092', 'ai.settings.manage', 'ai', 'ai.settings.manage')
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
            SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
            FROM barang.roles r CROSS JOIN barang.permissions p
            WHERE r.code = 'system.administrator' AND p.code IN ('ai.settings.read', 'ai.settings.manage')
            ON CONFLICT (role_id, permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM barang.role_permissions WHERE permission_id IN ('00000000-0000-0000-0000-000000000091','00000000-0000-0000-0000-000000000092');
            DELETE FROM barang.permissions WHERE id IN ('00000000-0000-0000-0000-000000000091','00000000-0000-0000-0000-000000000092');
            DROP TABLE IF EXISTS barang.ai_chat_messages;
            DROP TABLE IF EXISTS barang.ai_conversations;
            DROP TABLE IF EXISTS barang.ai_model_settings;
            DROP TABLE IF EXISTS barang.ai_audit_log;
            """);
    }
}
