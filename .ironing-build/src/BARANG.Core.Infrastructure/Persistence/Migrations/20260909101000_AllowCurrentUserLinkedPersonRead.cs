using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CoreDbContext))]
[Migration("20260909101000_AllowCurrentUserLinkedPersonRead")]
public sealed class AllowCurrentUserLinkedPersonRead : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE POLICY persons_current_user_identity_read
                ON barang.persons
                FOR SELECT
                USING (
                    EXISTS (
                        SELECT 1
                        FROM barang.application_users AS current_user_account
                        WHERE current_user_account.id = NULLIF(current_setting('barang.user_id', true), '')::uuid
                          AND current_user_account.person_id = persons.id
                    )
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP POLICY IF EXISTS persons_current_user_identity_read ON barang.persons;");
    }
}
