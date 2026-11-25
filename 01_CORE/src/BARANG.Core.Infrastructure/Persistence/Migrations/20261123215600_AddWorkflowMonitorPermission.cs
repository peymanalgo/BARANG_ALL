using Microsoft.EntityFrameworkCore.Migrations;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// BPMS monitoring permission (workflow.monitor): seeds the permission row (stable
/// GUID from the SecurityPermissions.Catalog order) and grants it to the
/// system.administrator role. Idempotent SQL so it cannot conflict with sibling
/// permission-seed migrations generated in the same window.
/// </summary>
[Migration("20261123215600_AddWorkflowMonitorPermission")]
[DbContext(typeof(CoreDbContext))]
public partial class AddWorkflowMonitorPermissionSeed : Migration
{
    private const string PermissionId = "00000000-0000-0000-0000-000000000093";
    private const string PermissionCode = "workflow.monitor";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            INSERT INTO barang.permissions(id,code,module,technical_description)
            VALUES ('{PermissionId}','{PermissionCode}','workflow','{PermissionCode}')
            ON CONFLICT (code) DO NOTHING;
            INSERT INTO barang.role_permissions(id,role_id,permission_id,assigned_at_utc,assigned_by_user_id)
            SELECT md5(r.id::text||p.id::text)::uuid,r.id,p.id,CURRENT_TIMESTAMP,NULL
            FROM barang.roles r CROSS JOIN barang.permissions p
            WHERE r.code='system.administrator' AND p.code='{PermissionCode}'
            ON CONFLICT (role_id,permission_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DELETE FROM barang.role_permissions WHERE permission_id='{PermissionId}'; DELETE FROM barang.permissions WHERE id='{PermissionId}' AND code='{PermissionCode}';");
    }
}
