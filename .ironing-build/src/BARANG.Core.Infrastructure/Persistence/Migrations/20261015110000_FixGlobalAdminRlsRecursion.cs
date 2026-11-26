using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Removes the RLS recursion in the administrator policy. The predicate is
    /// intentionally limited to the active administrator role assignment for
    /// the explicitly selected company; membership reads remain governed by the
    /// normal self/company policies.
    /// </summary>
    public partial class FixGlobalAdminRlsRecursion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION barang.reference_global_admin() RETURNS boolean
                LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
                SELECT EXISTS(
                 SELECT 1
                 FROM barang.application_users u
                 JOIN barang.user_roles a ON a.user_id=u.id
                 JOIN barang.roles r ON r.id=a.role_id
                 WHERE u.id=NULLIF(current_setting('barang.user_id',true),'')::uuid
                   AND u.account_state='Active'
                   AND r.code='system.administrator'
                   AND r.is_active
                   AND a.valid_from_utc<=CURRENT_TIMESTAMP
                   AND (a.valid_to_utc IS NULL OR a.valid_to_utc>CURRENT_TIMESTAMP)
                );
                $fn$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION barang.reference_global_admin() RETURNS boolean
                LANGUAGE sql STABLE SECURITY DEFINER SET search_path=pg_catalog,barang AS $fn$
                SELECT EXISTS(
                 SELECT 1 FROM barang.application_users u
                 JOIN barang.user_roles a ON a.user_id=u.id
                 JOIN barang.user_companies access ON access.user_id=a.user_id AND access.company_id=a.company_id
                 JOIN barang.roles r ON r.id=a.role_id
                 WHERE u.id=NULLIF(current_setting('barang.user_id',true),'')::uuid
                   AND u.account_state='Active' AND r.code='system.administrator' AND r.is_active
                   AND a.valid_from_utc<=CURRENT_TIMESTAMP AND (a.valid_to_utc IS NULL OR a.valid_to_utc>CURRENT_TIMESTAMP)
                   AND access.valid_from_utc<=CURRENT_TIMESTAMP AND (access.valid_to_utc IS NULL OR access.valid_to_utc>CURRENT_TIMESTAMP)
                );
                $fn$;
                """);
        }
    }
}
