using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

public partial class ProbeModelAlignment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Earlier authoritative migrations create the canonical lowercase organization tables.
        // This migration advances the model snapshot only and performs no data movement.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}

