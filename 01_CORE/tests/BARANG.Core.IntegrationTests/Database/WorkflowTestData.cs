using Npgsql;

namespace BARANG.Core.IntegrationTests.Database;

internal static class WorkflowTestData
{
    public static async Task SeedPublishedOrderDefinitionAsync(string connectionString, Guid companyId, string username, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (NpgsqlCommand context = new("select set_config('barang.company_id', @company, false)", connection, transaction))
        {
            context.Parameters.AddWithValue("company", companyId.ToString());
            await context.ExecuteNonQueryAsync(cancellationToken);
        }

        Guid definition = Guid.NewGuid(), version = Guid.NewGuid(), start = Guid.NewGuid(), task = Guid.NewGuid(), transition = Guid.NewGuid();
        await using (NpgsqlCommand existing = new("select 1 from barang.workflow_definitions where company_id=@company and code='BARANG.ORDER'", connection, transaction))
        {
            existing.Parameters.AddWithValue("company", companyId);
            if (await existing.ExecuteScalarAsync(cancellationToken) is not null) { await transaction.CommitAsync(cancellationToken); return; }
        }

        Guid actor;
        await using (NpgsqlCommand user = new("select id from barang.application_users where upper(username)=upper(@username)", connection, transaction))
        {
            user.Parameters.AddWithValue("username", username);
            actor = (Guid)(await user.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Workflow test user was not created."));
        }

        const string sql = """
            insert into barang.workflow_definitions(id,company_id,code,name_json,created_at_utc,updated_at_utc,version)
            values(@definition,@company,'BARANG.ORDER',@name::jsonb,@now,@now,1);
            insert into barang.workflow_definition_versions(id,company_id,definition_id,version_number,status,change_note_json,created_at_utc,published_at_utc,created_by_user_id,published_by_user_id,version)
            values(@version,@company,@definition,1,'Active','{}'::jsonb,@now,@now,@actor,@actor,1);
            update barang.workflow_definitions set active_version_id=@version where company_id=@company and id=@definition;
            insert into barang.workflow_definition_nodes(id,company_id,definition_version_id,node_key,type,title_json,description_json,x,y,version)
            values(@start,@company,@version,'start','Start',@start_title::jsonb,'{}'::jsonb,0,0,1),
                  (@task,@company,@version,'review','UserTask',@task_title::jsonb,'{}'::jsonb,100,0,1);
            insert into barang.workflow_definition_transitions(id,company_id,definition_version_id,from_node_id,to_node_id,condition_json,is_default,sort_order,version)
            values(@transition,@company,@version,@start,@task,'{}'::jsonb,true,0,1);
            """;
        await using NpgsqlCommand command = new(sql, connection, transaction);
        command.Parameters.AddWithValue("definition", definition);
        command.Parameters.AddWithValue("version", version);
        command.Parameters.AddWithValue("start", start);
        command.Parameters.AddWithValue("task", task);
        command.Parameters.AddWithValue("transition", transition);
        command.Parameters.AddWithValue("company", companyId);
        command.Parameters.AddWithValue("actor", actor);
        command.Parameters.AddWithValue("name", "{\"fa\":\"سفارش\",\"en\":\"Order\",\"ar\":\"طلب\"}");
        command.Parameters.AddWithValue("start_title", "{\"fa\":\"شروع\",\"en\":\"Start\",\"ar\":\"بدء\"}");
        command.Parameters.AddWithValue("task_title", "{\"fa\":\"بررسی\",\"en\":\"Review\",\"ar\":\"مراجعة\"}");
        command.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
