using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

public partial class SupplierContractAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "file_id",
            schema: "barang",
            table: "supplier_contracts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "file_version_id",
            schema: "barang",
            table: "supplier_contracts",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "file_id", schema: "barang", table: "supplier_contracts");
        migrationBuilder.DropColumn(name: "file_version_id", schema: "barang", table: "supplier_contracts");
    }
}
