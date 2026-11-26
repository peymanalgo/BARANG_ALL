using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

// The equipment-module migrations already created the snake_case columns.
// This metadata-only migration records the corrected EF mapping in the
// snapshot without renaming or dropping existing development data.
public partial class EquipmentMappingMetadataAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { }
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
