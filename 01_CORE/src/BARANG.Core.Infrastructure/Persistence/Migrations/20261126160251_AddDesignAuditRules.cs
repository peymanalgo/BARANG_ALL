using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861 // EF migration column arrays are generated migration metadata.
#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignAuditRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "design_audit_rules",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    title_fa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title_es = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    @params = table.Column<string>(name: "params", type: "jsonb", maxLength: 2000, nullable: false),
                    print_size_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_design_audit_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_design_audit_rules_company_id",
                schema: "barang",
                table: "design_audit_rules",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "uq_design_audit_rules_company_code",
                schema: "barang",
                table: "design_audit_rules",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.Sql("""
                -- Permission seed (stable catalog GUID ...0095, appended at the END of SecurityPermissions.Catalog)
                INSERT INTO barang.permissions (id, code, module, technical_description)
                VALUES ('00000000-0000-0000-0000-000000000095', 'designaudit.manage', 'designaudit', 'designaudit.manage')
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT md5(r.id::text || p.id::text)::uuid, r.id, p.id, CURRENT_TIMESTAMP, NULL
                FROM barang.roles r CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator' AND p.code = 'designaudit.manage'
                ON CONFLICT (role_id, permission_id) DO NOTHING;
                """);

            migrationBuilder.Sql("""
                -- Seed قوانین ممیزی پیش‌فرض برای هر شرکت (۵ قانون چاپ)
                INSERT INTO barang.design_audit_rules
                    (id, company_id, code, rule_type, title_fa, title_en, title_ar, title_es,
                     params, print_size_code, is_active, created_at_utc, updated_at_utc, version)
                SELECT md5(c.id::text || '-dar-' || r.code)::uuid, c.id, r.code, r.rule_type,
                       r.title_fa, r.title_en, r.title_ar, r.title_es,
                       r.params::jsonb, r.print_size_code, true, now(), now(), 1
                FROM barang.companies c
                CROSS JOIN (VALUES
                    ('res-a4',  'min_resolution', 'حداقل رزولوشن چاپ (A4)', 'Minimum print resolution (A4)', 'الحد الأدنى لدقة الطباعة (A4)', 'Resolución mínima de impresión (A4)', '{"minPx":800}', 'A4'),
                    ('res-a3',  'min_resolution', 'حداقل رزولوشن چاپ (A3)', 'Minimum print resolution (A3)', 'الحد الأدنى لدقة الطباعة (A3)', 'Resolución mínima de impresión (A3)', '{"minPx":1400}', 'A3'),
                    ('res-a5',  'min_resolution', 'حداقل رزولوشن چاپ (A5)', 'Minimum print resolution (A5)', 'الحد الأدنى لدقة الطباعة (A5)', 'Resolución mínima de impresión (A5)', '{"minPx":600}', 'A5'),
                    ('text-safe', 'text_in_bounds', 'متن داخل ناحیه چاپ', 'Text inside print area', 'النص داخل منطقة الطباعة', 'Texto dentro del área de impresión', '{"safeMarginMm":5}', NULL),
                    ('contrast', 'contrast', 'کنتراست متن با رنگ پارچه', 'Text contrast with fabric', 'تباين النص مع لون القماش', 'Contraste del texto con la tela', '{"minDelta":0.15}', NULL)
                ) AS r(code, rule_type, title_fa, title_en, title_ar, title_es, params, print_size_code)
                WHERE NOT EXISTS (
                    SELECT 1 FROM barang.design_audit_rules d
                    WHERE d.company_id = c.id AND d.code = r.code);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "design_audit_rules",
                schema: "barang");
        }
    }
}
