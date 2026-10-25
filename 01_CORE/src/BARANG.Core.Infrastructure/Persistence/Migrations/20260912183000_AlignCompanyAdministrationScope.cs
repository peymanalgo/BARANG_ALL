using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260912183000_AlignCompanyAdministrationScope")]
public sealed class AlignCompanyAdministrationScope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Reuse the live, server-owned administrator predicate. Ordinary company RLS is unchanged.
        migrationBuilder.Sql("""
            DROP POLICY user_companies_system_administrator_management ON barang.user_companies;
            CREATE POLICY user_companies_system_administrator_management ON barang.user_companies FOR ALL
                USING (barang.reference_global_admin()) WITH CHECK (barang.reference_global_admin());
            DROP POLICY audit_events_system_administrator_append ON barang.audit_events;
            CREATE POLICY audit_events_system_administrator_append ON barang.audit_events FOR INSERT
                WITH CHECK (barang.reference_global_admin());
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Restore the prior reviewed administrator policy with a forward migration.");
}
