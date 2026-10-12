using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// Reconciles the historical late evidence migration with the three migrations
/// that already own these objects: personal follow-up, decision attachments,
/// and substitutions/forward context.
/// </summary>
public partial class LateWorkflowEvidenceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty. The original migration duplicated objects created
        // by 20261007120000, 20261007150000, and 20261007163000.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Those objects belong to their owning migrations and must not be dropped here.
    }
}
