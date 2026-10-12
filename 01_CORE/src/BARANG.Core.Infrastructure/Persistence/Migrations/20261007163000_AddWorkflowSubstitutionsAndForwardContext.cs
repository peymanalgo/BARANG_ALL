using Microsoft.EntityFrameworkCore.Migrations;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[Migration("20261007163000_AddWorkflowSubstitutionsAndForwardContext")]
[DbContext(typeof(CoreDbContext))]
public partial class AddWorkflowSubstitutionsAndForwardContext : Migration
{
    private static readonly string[] PrincipalIndexColumns = ["company_id", "principal_user_id", "effective_from_utc", "effective_to_utc"];
    private static readonly string[] SubstituteIndexColumns = ["company_id", "substitute_user_id", "effective_from_utc", "effective_to_utc"];
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "claimed_on_behalf_of_user_id",
            schema: "barang",
            table: "workflow_runtime_tasks",
            type: "uuid",
            nullable: true);
        migrationBuilder.CreateTable(
            name: "workflow_substitutions",
            schema: "barang",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                company_id = table.Column<Guid>(type: "uuid", nullable: false),
                version = table.Column<long>(type: "bigint", nullable: false),
                principal_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                substitute_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                effective_from_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                effective_to_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_workflow_substitutions", x => x.id);
                table.UniqueConstraint("ak_workflow_substitutions_company_id_id", x => new { x.company_id, x.id });
                table.ForeignKey("fk_workflow_substitutions_principal_user", x => x.principal_user_id, "application_users", "id", "barang", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_workflow_substitutions_substitute_user", x => x.substitute_user_id, "application_users", "id", "barang", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "ix_workflow_substitutions_company_principal_period", schema: "barang", table: "workflow_substitutions", columns: PrincipalIndexColumns);
        migrationBuilder.CreateIndex(name: "ix_workflow_substitutions_company_substitute_period", schema: "barang", table: "workflow_substitutions", columns: SubstituteIndexColumns);
        migrationBuilder.Sql("ALTER TABLE barang.workflow_substitutions ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql("DROP POLICY IF EXISTS workflow_substitutions_company_isolation ON barang.workflow_substitutions;");
        migrationBuilder.Sql("CREATE POLICY workflow_substitutions_company_isolation ON barang.workflow_substitutions USING (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid) WITH CHECK (company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid);");
        migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE barang.workflow_substitutions TO barang_app;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("workflow_substitutions", "barang");
        migrationBuilder.DropColumn("claimed_on_behalf_of_user_id", "workflow_runtime_tasks", "barang");
    }
}
