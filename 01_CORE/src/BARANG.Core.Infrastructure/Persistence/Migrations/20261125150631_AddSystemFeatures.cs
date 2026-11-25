using System;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable CA1861 // EF migration column arrays are generated migration metadata.

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// System feature flags («فیچرهای سیستم» — System Management): barang.system_features
    /// (unique per company + feature_code + scope, CHECK scope IN ('erp','store'), 4-language
    /// name/description columns, optimistic version). Seeds «دستیار هوشمند» (ai.assistant)
    /// for BOTH scopes (erp + store) for every existing company, plus the features.manage
    /// permission seed (stable catalog GUID ...0094, appended at the END of
    /// SecurityPermissions.Catalog) granted to the system.administrator role.
    /// Seed/grant steps use idempotent SQL so they cannot conflict with sibling migrations
    /// generated in the same window.
    /// </summary>
    public partial class AddSystemFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "system_features",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    name_fa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    name_es = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    description_fa = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false, defaultValue: ""),
                    description_en = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false, defaultValue: ""),
                    description_ar = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false, defaultValue: ""),
                    description_es = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false, defaultValue: ""),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_features", x => x.id);
                    table.CheckConstraint("ck_system_features_scope_valid", "scope IN ('erp','store')");
                    table.CheckConstraint("ck_system_features_version_positive", "version > 0");
                });

            migrationBuilder.Sql("""
                -- Permission seed (stable catalog GUID ...0094, appended at the END of SecurityPermissions.Catalog)
                INSERT INTO barang.permissions (id, code, module, technical_description)
                VALUES ('00000000-0000-0000-0000-000000000094', 'features.manage', 'features', 'features.manage')
                ON CONFLICT (code) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                -- Seed «دستیار هوشمند» (ai.assistant) برای هر شرکت موجود — هر دو اسکوپ (ERP + فروشگاه پی‌رنگ)، روشن
                INSERT INTO barang.system_features
                    (id, company_id, feature_code, scope, name_fa, name_en, name_ar, name_es,
                     description_fa, description_en, description_ar, description_es,
                     is_enabled, created_at_utc, updated_at_utc, version)
                SELECT md5(c.id::text || '-ai.assistant-' || s.scope)::uuid,
                       c.id, 'ai.assistant', s.scope,
                       'دستیار هوشمند', 'AI assistant', 'المساعد الذكي', 'Asistente inteligente',
                       'گفتگو با دستیار هوشمند بر پایه مدل زبانی در این سایت',
                       'Chat with the language-model powered smart assistant on this site',
                       'الدردشة مع المساعد الذكي المدعوم بنموذج اللغة في هذا الموقع',
                       'Chatea con el asistente inteligente basado en el modelo de lenguaje de este sitio',
                       true, now(), now(), 1
                FROM barang.companies c
                CROSS JOIN (VALUES ('erp'), ('store')) AS s(scope)
                WHERE NOT EXISTS (
                    SELECT 1 FROM barang.system_features sf
                    WHERE sf.company_id = c.id AND sf.feature_code = 'ai.assistant' AND sf.scope = s.scope);

                -- Grant features.manage to system.administrator role (idempotent)
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator' AND p.code = 'features.manage'
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_system_features_company_id",
                schema: "barang",
                table: "system_features",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_system_features_company_code_scope",
                schema: "barang",
                table: "system_features",
                columns: new[] { "company_id", "feature_code", "scope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "system_features",
                schema: "barang");

            migrationBuilder.Sql("""
                DELETE FROM barang.role_permissions WHERE permission_id = '00000000-0000-0000-0000-000000000094';
                """);

            migrationBuilder.DeleteData(
                schema: "barang",
                table: "permissions",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000094"));
        }
    }
}
