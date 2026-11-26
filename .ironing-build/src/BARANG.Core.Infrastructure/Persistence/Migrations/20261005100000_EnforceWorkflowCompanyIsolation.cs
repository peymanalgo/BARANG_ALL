using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BARANG.Core.Infrastructure.Persistence.Migrations;

/// <summary>
/// Completes the PostgreSQL company-isolation boundary for the workflow designer
/// and definition-runtime tables introduced after the original workflow migration.
/// The EF query filters remain useful for application composition, but RLS is the
/// database-enforced boundary and therefore also protects direct API/query paths.
/// </summary>
public partial class EnforceWorkflowCompanyIsolation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $workflow_rls$
            DECLARE table_name text;
            BEGIN
                FOREACH table_name IN ARRAY ARRAY[
                    'workflow_definitions',
                    'workflow_definition_versions',
                    'workflow_definition_nodes',
                    'workflow_definition_transitions',
                    'workflow_execution_tokens',
                    'workflow_runtime_tasks'
                ] LOOP
                    EXECUTE format('ALTER TABLE barang.%I ENABLE ROW LEVEL SECURITY', table_name);
                    EXECUTE format('DROP POLICY IF EXISTS %I ON barang.%I', table_name || '_company_isolation', table_name);
                    EXECUTE format(
                        'CREATE POLICY %I ON barang.%I USING (company_id = NULLIF(current_setting(''barang.company_id'', true), '''')::uuid) WITH CHECK (company_id = NULLIF(current_setting(''barang.company_id'', true), '''')::uuid)',
                        table_name || '_company_isolation', table_name);
                END LOOP;
            END $workflow_rls$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $workflow_rls_down$
            DECLARE table_name text;
            BEGIN
                FOREACH table_name IN ARRAY ARRAY[
                    'workflow_definitions',
                    'workflow_definition_versions',
                    'workflow_definition_nodes',
                    'workflow_definition_transitions',
                    'workflow_execution_tokens',
                    'workflow_runtime_tasks'
                ] LOOP
                    EXECUTE format('DROP POLICY IF EXISTS %I ON barang.%I', table_name || '_company_isolation', table_name);
                    EXECUTE format('ALTER TABLE barang.%I DISABLE ROW LEVEL SECURITY', table_name);
                END LOOP;
            END $workflow_rls_down$;
            """);
    }
}
