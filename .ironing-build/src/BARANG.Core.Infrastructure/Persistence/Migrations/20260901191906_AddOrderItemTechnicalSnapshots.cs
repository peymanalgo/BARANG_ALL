using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemTechnicalSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_order_items_id_company_id",
                schema: "barang",
                table: "order_items",
                columns: new[] { "id", "company_id" });

            migrationBuilder.CreateTable(
                name: "order_item_technical_snapshots",
                schema: "barang",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    material_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    gender_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fit_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    style_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    technical_attributes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item_technical_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_item_technical_snapshots_order_items_order_item_id_co~",
                        columns: x => new { x.order_item_id, x.company_id },
                        principalSchema: "barang",
                        principalTable: "order_items",
                        principalColumns: new[] { "id", "company_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_item_technical_snapshots_company_id_order_item_id",
                schema: "barang",
                table: "order_item_technical_snapshots",
                columns: new[] { "company_id", "order_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_item_technical_snapshots_order_item_id_company_id",
                schema: "barang",
                table: "order_item_technical_snapshots",
                columns: new[] { "order_item_id", "company_id" });
            migrationBuilder.Sql("""
                ALTER TABLE barang.order_item_technical_snapshots ENABLE ROW LEVEL SECURITY;
                CREATE POLICY order_item_technical_snapshots_company_isolation ON barang.order_item_technical_snapshots USING(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid) WITH CHECK(company_id=NULLIF(current_setting('barang.company_id',true),'')::uuid);
                DO $grant$ BEGIN IF EXISTS(SELECT 1 FROM pg_roles WHERE rolname='barang_app') THEN GRANT SELECT,INSERT ON barang.order_item_technical_snapshots TO barang_app; REVOKE UPDATE,DELETE ON barang.order_item_technical_snapshots FROM barang_app; END IF; END $grant$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item_technical_snapshots",
                schema: "barang");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_order_items_id_company_id",
                schema: "barang",
                table: "order_items");
        }
    }
}
