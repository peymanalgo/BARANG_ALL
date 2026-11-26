using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityInspectionDispositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "qc_inspection_dispositions",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspectable_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    accepted_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    rejected_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reproduction_required_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    accepted_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    rejected_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reproduction_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qc_inspection_dispositions", x => x.id);
                    table.ForeignKey(
                        name: "FK_qc_inspection_dispositions_qc_inspections_company_id_inspec~",
                        columns: x => new { x.company_id, x.inspection_id },
                        principalSchema: "barang",
                        principalTable: "qc_inspections",
                        principalColumns: new[] { "company_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_qc_inspection_dispositions_company_id_inspection_id",
                schema: "barang",
                table: "qc_inspection_dispositions",
                columns: new[] { "company_id", "inspection_id" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE barang.qc_inspection_dispositions ENABLE ROW LEVEL SECURITY;
                CREATE POLICY qc_inspection_dispositions_company_isolation ON barang.qc_inspection_dispositions
                  USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid)
                  WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN
                    GRANT SELECT,INSERT,UPDATE ON barang.qc_inspection_dispositions TO barang_app;
                    REVOKE DELETE ON barang.qc_inspection_dispositions FROM barang_app;
                  END IF;
                  IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_test_app') THEN
                    GRANT SELECT,INSERT,UPDATE ON barang.qc_inspection_dispositions TO barang_test_app;
                    REVOKE DELETE ON barang.qc_inspection_dispositions FROM barang_test_app;
                  END IF;
                END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "qc_inspection_dispositions",
                schema: "barang");
        }
    }
}
