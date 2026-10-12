using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260909101100_EnableSystemAdministratorCompanyAccessManagement")]
public sealed class EnableSystemAdministratorCompanyAccessManagement : Migration
{
    private const string EffectiveSystemAdministrator =
        """
        EXISTS (
            SELECT 1
            FROM barang.user_roles AS administrator_assignment
            JOIN barang.roles AS administrator_role ON administrator_role.id = administrator_assignment.role_id
            WHERE administrator_assignment.user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid
              AND administrator_assignment.company_id = NULLIF(current_setting('barang.company_id', true), '')::uuid
              AND administrator_assignment.valid_from_utc <= now()
              AND (administrator_assignment.valid_to_utc IS NULL OR administrator_assignment.valid_to_utc > now())
              AND administrator_role.code = 'system.administrator'
              AND administrator_role.is_active
        )
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            $$"""
            CREATE POLICY user_companies_system_administrator_management
                ON barang.user_companies
                FOR ALL
                USING ({{EffectiveSystemAdministrator}})
                WITH CHECK ({{EffectiveSystemAdministrator}});

            CREATE POLICY audit_events_system_administrator_append
                ON barang.audit_events
                FOR INSERT
                WITH CHECK ({{EffectiveSystemAdministrator}});
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP POLICY IF EXISTS audit_events_system_administrator_append ON barang.audit_events;
            DROP POLICY IF EXISTS user_companies_system_administrator_management ON barang.user_companies;
            """);
    }
}
