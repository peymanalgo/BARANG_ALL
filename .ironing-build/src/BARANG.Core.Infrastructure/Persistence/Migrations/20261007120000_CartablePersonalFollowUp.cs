using Microsoft.EntityFrameworkCore.Migrations;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable
#pragma warning disable CA1861

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[Migration("20261007120000_CartablePersonalFollowUp")]
[DbContext(typeof(CoreDbContext))]
public partial class CartablePersonalFollowUp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset?>(name: "follow_up_at_utc", schema: "barang", table: "cartable_personal_states", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex(name: "ix_cartable_personal_states_company_user_follow_up", schema: "barang", table: "cartable_personal_states", columns: new[] { "company_id", "user_id", "follow_up_at_utc", "runtime_task_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_cartable_personal_states_company_user_follow_up", schema: "barang", table: "cartable_personal_states");
        migrationBuilder.DropColumn(name: "follow_up_at_utc", schema: "barang", table: "cartable_personal_states");
    }
}
