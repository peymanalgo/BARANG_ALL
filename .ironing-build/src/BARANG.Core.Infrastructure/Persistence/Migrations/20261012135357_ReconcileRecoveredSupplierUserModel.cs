using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// Advances the EF snapshot to the recovered Supplier/User model.
/// The preceding migrations already own the physical objects; this migration is intentionally data-preserving.
/// Historical Workflow tables are not represented by the current DbContext and are therefore not dropped.
/// </summary>
public partial class ReconcileRecoveredSupplierUserModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
