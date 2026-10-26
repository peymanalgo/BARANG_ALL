using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkShiftsAndEquipmentCapacity : Migration
    {
        private static readonly string[] ShiftIndexColumns = ["company_id", "is_active"];
        private static readonly string[] ShiftUniqueColumns = ["company_id", "code"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "capacity_per_hour",
                schema: "barang",
                table: "equipment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "work_shifts",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_shifts", x => x.id);
                    table.CheckConstraint("ck_work_shifts_version_positive", "version > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_shifts_company_id_is_active",
                schema: "barang",
                table: "work_shifts",
                columns: ShiftIndexColumns);

            migrationBuilder.CreateIndex(
                name: "uq_work_shifts_company_code",
                schema: "barang",
                table: "work_shifts",
                columns: ShiftUniqueColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_shifts",
                schema: "barang");

            migrationBuilder.DropColumn(
                name: "capacity_per_hour",
                schema: "barang",
                table: "equipment");
        }
    }
}
