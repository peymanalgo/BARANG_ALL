using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GrantAiSettingsToAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Grant ai.settings.read + ai.settings.manage to every system.administrator role (idempotent).
            migrationBuilder.Sql("""
                INSERT INTO barang.role_permissions (id, role_id, permission_id, assigned_at_utc, assigned_by_user_id)
                SELECT gen_random_uuid(), r.id, p.id, now(), NULL
                FROM barang.roles r
                CROSS JOIN barang.permissions p
                WHERE r.code = 'system.administrator'
                  AND p.code IN ('ai.settings.read', 'ai.settings.manage')
                  AND NOT EXISTS (SELECT 1 FROM barang.role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM barang.role_permissions rp
                USING barang.permissions p
                WHERE rp.permission_id = p.id AND p.code IN ('ai.settings.read', 'ai.settings.manage');
                """);
        }
    }
}
