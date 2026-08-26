using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260826120000_AddAuthenticatedSelfServiceContext")]
public sealed class AddAuthenticatedSelfServiceContext : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE POLICY user_companies_authenticated_self_read
            ON barang.user_companies
            FOR SELECT
            USING (user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid);

            CREATE POLICY user_roles_authenticated_self_read
            ON barang.user_roles
            FOR SELECT
            USING (user_id = NULLIF(current_setting('barang.user_id', true), '')::uuid);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP POLICY IF EXISTS user_roles_authenticated_self_read ON barang.user_roles;
            DROP POLICY IF EXISTS user_companies_authenticated_self_read ON barang.user_companies;
            """);
    }
}
