using System.Data;
using System.Data.Common;
using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BARANG.Core.Infrastructure.Workflow;

public sealed class WorkflowDesignerAdministration(CoreDbContext db, ICompanyContext company, ICurrentActor actor, TimeProvider clock) : IWorkflowDesignerAdministration
{
    private Guid Company => company.CompanyId is { } id && id != Guid.Empty ? id : throw new InvalidOperationException("Company context is required.");
    private DateTimeOffset Now => clock.GetUtcNow();
    private Guid Actor => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : throw new InvalidOperationException("Actor context is required.");
    public IReadOnlyList<object> ConditionFields { get; } = [
        new { code = "Order.CustomerType", title = new { fa = "نوع مشتری", en = "Customer type", ar = "نوع العميل" } },
        new { code = "Order.Source", title = new { fa = "منبع سفارش", en = "Order source", ar = "مصدر الطلب" } },
        new { code = "Order.Total", title = new { fa = "مبلغ سفارش", en = "Order total", ar = "إجمالي الطلب" } }];

    public async Task<Result<object>> GetAsync(Guid? versionId, CancellationToken ct)
    {
        await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct);
        Guid definition; string code; JsonElement name; Guid? active;
        await using (DbCommand q = Cmd(c, "select id,code,name_json,active_version_id from barang.workflow_definitions where company_id=@company and code='BARANG.ORDER'", ("company", Company)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        { if (!await r.ReadAsync(ct)) return Result.Failure<object>(Error.NotFound("workflow.definition.not_found")); definition = r.GetGuid(0); code = r.GetString(1); using JsonDocument n = JsonDocument.Parse(r.GetString(2)); name = n.RootElement.Clone(); active = r.IsDBNull(3) ? null : r.GetGuid(3); }
        Guid selected = versionId ?? active ?? Guid.Empty; if (selected == Guid.Empty) return Result.Failure<object>(Error.Validation("workflow.definition.no_active_version"));
        object? current = await ReadVersion(c, definition, selected, ct); if (current is null) return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found"));
        List<object> history = []; await using DbCommand h = Cmd(c, "select id,version_number,status,created_at_utc from barang.workflow_definition_versions where company_id=@company and definition_id=@definition order by version_number desc", ("company", Company), ("definition", definition)); await using DbDataReader hr = await h.ExecuteReaderAsync(ct); while (await hr.ReadAsync(ct)) history.Add(new { id = hr.GetGuid(0), number = hr.GetInt32(1), status = hr.GetString(2), createdAtUtc = hr.GetFieldValue<DateTimeOffset>(3), isReadOnly = hr.GetString(2) != "Draft" || hr.GetGuid(0) != selected });
        return Result.Success<object>(new { current, history, definition = new { id = definition, code, name } });
    }

    public async Task<Result<object>> ImportAsync(WorkflowDefinitionImportCommand command, CancellationToken ct)
    {
        if (!string.Equals(command.Code, "BARANG.ORDER", StringComparison.Ordinal))
            return Result.Failure<object>(Error.Validation("workflow.definition.import_code_not_supported"));
        WorkflowGraphCommand graph = new(null, command.Name, command.ChangeNote, command.Nodes, command.Transitions);
        List<object> issues = Validate(graph);
        if (issues.Count > 0) return Result.Failure<object>(Error.Validation("workflow.definition.invalid_graph"));

        await using DbConnection c = db.Database.GetDbConnection();
        await c.OpenAsync(ct);
        await SetCompany(c, ct);
        await using DbTransaction tx = await c.BeginTransactionAsync(ct);
        await using (DbCommand exists = CmdTx(c, "select id from barang.workflow_definitions where company_id=@company and code=@code for update", ("company", Company), ("code", command.Code), tx))
        {
            if (await exists.ExecuteScalarAsync(ct) is Guid existingDefinition)
            {
                await using DbCommand draft = CmdTx(c, "select id from barang.workflow_definition_versions where company_id=@company and definition_id=@definition and status='Draft' order by version_number desc limit 1", ("company", Company), ("definition", existingDefinition), tx);
                if (await draft.ExecuteScalarAsync(ct) is Guid existingDraft)
                {
                    await tx.RollbackAsync(ct);
                    await c.CloseAsync();
                    return await GetAsync(existingDraft, ct);
                }
                await tx.RollbackAsync(ct);
                return Result.Failure<object>(Error.Conflict("workflow.definition.already_exists"));
            }
        }

        Dictionary<string, Guid?> mappedPositions = new(StringComparer.OrdinalIgnoreCase);
        foreach (WorkflowGraphNode node in command.Nodes.Where(x => x.PositionId is not null))
        {
            string key = node.PositionId!.Value.ToString();
            if (command.PositionCodes is null || !command.PositionCodes.TryGetValue(key, out string? code) || string.IsNullOrWhiteSpace(code))
            {
                await tx.RollbackAsync(ct);
                return Result.Failure<object>(Error.Validation("workflow.definition.import_position_mapping_required"));
            }
            await using DbCommand position = CmdTx(c, "select id from barang.positions where company_id=@company and code=@code and is_active=true", ("company", Company), ("code", code.Trim()), tx);
            object? value = await position.ExecuteScalarAsync(ct);
            if (value is null)
            {
                await tx.RollbackAsync(ct);
                return Result.Failure<object>(Error.BusinessRule("workflow.definition.import_position_not_found"));
            }
            mappedPositions[key] = (Guid)value;
        }

        Guid definition = Guid.NewGuid();
        Guid version = Guid.NewGuid();
        DateTimeOffset now = Now;
        await using (DbCommand insert = CmdTx(c, "insert into barang.workflow_definitions(id,company_id,code,name_json,created_at_utc,updated_at_utc,version) values(@id,@company,@code,@name::jsonb,@now,@now,1)",
            ("id", definition), ("company", Company), ("code", command.Code), ("name", JsonSerializer.Serialize(command.Name)), ("now", now), tx))
            await insert.ExecuteNonQueryAsync(ct);
        await using (DbCommand insertVersion = CmdTx(c, "insert into barang.workflow_definition_versions(id,company_id,definition_id,version_number,status,change_note_json,created_at_utc,created_by_user_id,version) values(@version_id,@company,@definition_id,1,'Draft',@note::jsonb,@now,@actor,1)",
            ("version_id", version), ("company", Company), ("definition_id", definition), ("note", JsonSerializer.Serialize(command.ChangeNote ?? new("", "", ""))), ("now", now), ("actor", Actor), tx))
            await insertVersion.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        await c.CloseAsync();

        IReadOnlyList<WorkflowGraphNode> nodes = command.Nodes.Select(node =>
            node.PositionId is { } source && mappedPositions.TryGetValue(source.ToString(), out Guid? target)
                ? node with { PositionId = target }
                : node).ToArray();
        Result<object> saved = await SaveDraftAsync(new WorkflowGraphCommand(version, command.Name, command.ChangeNote, nodes, command.Transitions), ct);
        if (saved.IsFailure)
        {
            await using DbConnection cleanup = db.Database.GetDbConnection();
            await cleanup.OpenAsync(ct);
            await SetCompany(cleanup, ct);
            await using DbCommand delete = Cmd(cleanup, "delete from barang.workflow_definitions where company_id=@company and id=@definition", ("company", Company), ("definition", definition));
            await delete.ExecuteNonQueryAsync(ct);
            await cleanup.CloseAsync();
        }
        return saved;
    }

    public async Task<Result<object>> SaveDraftAsync(WorkflowGraphCommand command, CancellationToken ct)
    {
        List<object> issues = Validate(command); if (issues.Count > 0) return Result.Failure<object>(Error.Validation("workflow.definition.invalid_graph"));
        await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbTransaction tx = await c.BeginTransactionAsync(ct);
        Guid definition; Guid version; int number; long expected = 0;
        bool definitionFound;
        await using (DbCommand q = CmdTx(c, "select id,active_version_id from barang.workflow_definitions where company_id=@company and code='BARANG.ORDER' for update", ("company", Company), tx))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        {
            definitionFound = await r.ReadAsync(ct);
            if (definitionFound)
            {
                definition = r.GetGuid(0);
                Guid? active = r.IsDBNull(1) ? null : r.GetGuid(1);
                version = command.VersionId ?? active ?? Guid.Empty;
            }
            else
            {
                definition = Guid.Empty;
                version = Guid.Empty;
            }
        }
        if (!definitionFound) { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.NotFound("workflow.definition.not_found")); }
        if (version == Guid.Empty) { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found")); }
        bool versionFound;
        string status = string.Empty;
        await using (DbCommand v = CmdTx(c, "select version_number,status,version from barang.workflow_definition_versions where company_id=@company and definition_id=@definition and id=@id for update", ("company", Company), ("definition", definition), ("id", version), tx))
        await using (DbDataReader vr = await v.ExecuteReaderAsync(ct))
        {
            versionFound = await vr.ReadAsync(ct);
            if (versionFound)
            {
                number = vr.GetInt32(0);
                status = vr.GetString(1);
                expected = vr.GetInt64(2);
            }
            else number = 0;
        }
        if (!versionFound) { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found")); }
        if (status != "Draft")
        {
            version = Guid.NewGuid();
            await using DbCommand next = CmdTx(c, "select coalesce(max(version_number),0)+1 from barang.workflow_definition_versions where company_id=@company and definition_id=@definition", ("company", Company), ("definition", definition), tx);
            number = Convert.ToInt32(await next.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
            await using DbCommand create = CmdTx(c, "insert into barang.workflow_definition_versions(id,company_id,definition_id,version_number,status,change_note_json,created_at_utc,created_by_user_id,version) values(@id,@company,@definition,@number,'Draft','{}'::jsonb,@now,@actor,1)", ("id", version), ("company", Company), ("definition", definition), ("number", number), ("now", Now), ("actor", Actor), tx);
            await create.ExecuteNonQueryAsync(ct);
            expected = 1;
        }
        await using (DbCommand delT = CmdTx(c, "delete from barang.workflow_definition_transitions where company_id=@company and definition_version_id=@version", ("company", Company), ("version", version), tx)) await delT.ExecuteNonQueryAsync(ct);
        await using (DbCommand delN = CmdTx(c, "delete from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version", ("company", Company), ("version", version), tx)) await delN.ExecuteNonQueryAsync(ct);
        Dictionary<string, Guid> ids = new(StringComparer.OrdinalIgnoreCase); foreach (WorkflowGraphNode n in command.Nodes) { Guid id = Guid.NewGuid(); ids[n.Key] = id; await using DbCommand ins = CmdTx(c, "insert into barang.workflow_definition_nodes(id,company_id,definition_version_id,node_key,type,title_json,description_json,position_id,x,y,version) values(@id,@company,@version,@key,@type,@title::jsonb,@description::jsonb,@position,@x,@y,1)", ("id", id), ("company", Company), ("version", version), ("key", n.Key.Trim()), ("type", TypeName(n.Type)), ("title", JsonSerializer.Serialize(n.Title ?? new("", "", ""))), ("description", JsonSerializer.Serialize(n.Description ?? new("", "", ""))), ("position", (object?)(n.PositionId is { } p && p != Guid.Empty ? p : null)), ("x", n.X), ("y", n.Y), tx); await ins.ExecuteNonQueryAsync(ct); }
        int sort = 0; foreach (WorkflowGraphTransition e in command.Transitions) { if (!ids.TryGetValue(e.FromKey, out Guid from) || !ids.TryGetValue(e.ToKey, out Guid to)) continue; string condition = JsonSerializer.Serialize(new { fieldCode = e.FieldCode, @operator = e.Operator, value = e.Value, outcome = e.OutcomeCode, sourceAnchor = e.SourceAnchor ?? "Auto", targetAnchor = e.TargetAnchor ?? "Auto", title = e.Title }); await using DbCommand ins = CmdTx(c, "insert into barang.workflow_definition_transitions(id,company_id,definition_version_id,from_node_id,to_node_id,condition_json,is_default,sort_order,version) values(@id,@company,@version,@from,@to,@condition::jsonb,@default,@sort,1)", ("id", Guid.NewGuid()), ("company", Company), ("version", version), ("from", from), ("to", to), ("condition", condition), ("default", e.IsDefault), ("sort", e.SortOrder == 0 ? sort++ : e.SortOrder), tx); await ins.ExecuteNonQueryAsync(ct); }
        await using (DbCommand bump = CmdTx(c, "update barang.workflow_definition_versions set change_note_json=@note::jsonb,version=version+1 where company_id=@company and id=@version and version=@expected", ("note", JsonSerializer.Serialize(command.ChangeNote ?? new("", "", ""))), ("company", Company), ("version", version), ("expected", expected), tx)) if (await bump.ExecuteNonQueryAsync(ct) != 1) { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.Concurrency("workflow.definition.concurrency")); }
        await tx.CommitAsync(ct); await c.CloseAsync(); return await GetAsync(version, ct);
    }

    public async Task<Result<IReadOnlyList<object>>> ValidateAsync(Guid versionId, CancellationToken ct)
    { await using DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); List<object> issues = []; await using DbCommand q = Cmd(c, "select count(*) filter(where type='Start'),count(*) filter(where type='End'),count(*) filter(where type='UserTask' and position_id is null and node_key not ilike '%InventoryIssue%') from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version", ("company", Company), ("version", versionId)); await using DbDataReader r = await q.ExecuteReaderAsync(ct); if (await r.ReadAsync(ct)) { if (r.GetInt64(0) != 1) issues.Add(new { nodeKey = "", messageKey = "ExactlyOneStart" }); if (r.GetInt64(1) < 1) issues.Add(new { nodeKey = "", messageKey = "AtLeastOneEnd" }); if (r.GetInt64(2) > 0) issues.Add(new { nodeKey = "", messageKey = "TaskPositionRequired" }); } return Result.Success<IReadOnlyList<object>>(issues); }
    public Task<Result<IReadOnlyList<object>>> ValidateAsync(WorkflowGraphCommand command, CancellationToken ct) => Task.FromResult(Result.Success<IReadOnlyList<object>>(Validate(command)));
    public async Task<Result<object>> PublishAsync(Guid versionId, CancellationToken ct)
    {
        await using DbConnection c = db.Database.GetDbConnection();
        await c.OpenAsync(ct); await SetCompany(c, ct);
        await using DbCommand validation = Cmd(c, "select count(*) filter(where type='Start'),count(*) filter(where type='End'),count(*) filter(where type='UserTask' and position_id is null and node_key not ilike '%InventoryIssue%') from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version", ("company", Company), ("version", versionId));
        await using DbDataReader validationReader = await validation.ExecuteReaderAsync(ct);
        if (await validationReader.ReadAsync(ct) && (validationReader.GetInt64(0) != 1 || validationReader.GetInt64(1) < 1 || validationReader.GetInt64(2) > 0)) return Result.Failure<object>(Error.Validation("workflow.definition.invalid_graph"));
        await validationReader.DisposeAsync();
        await using DbCommand q = Cmd(c, "select definition_id,version,status from barang.workflow_definition_versions where company_id=@company and id=@id for update", ("company", Company), ("id", versionId));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found"));
        Guid definition = r.GetGuid(0); long version = r.GetInt64(1); string status = r.GetString(2);
        if (status != "Draft") return Result.Failure<object>(Error.BusinessRule("workflow.definition.published_immutable"));
        await r.DisposeAsync();
        await using DbCommand u = Cmd(c, "update barang.workflow_definition_versions set status='Superseded',published_at_utc=coalesce(published_at_utc,now()),published_by_user_id=@actor where company_id=@company and definition_id=@definition and status='Active'; update barang.workflow_definition_versions set status='Active',published_at_utc=now(),published_by_user_id=@actor,version=version+1 where company_id=@company and id=@id and version=@version; update barang.workflow_definitions set active_version_id=@id,updated_at_utc=now(),version=version+1 where company_id=@company and id=@definition", ("actor", Actor), ("company", Company), ("definition", definition), ("id", versionId), ("version", version));
        await u.ExecuteNonQueryAsync(ct); await c.CloseAsync(); return await GetAsync(versionId, ct);
    }
    public async Task<Result<IReadOnlyList<object>>> PositionsAsync(string? search, int limit, CancellationToken ct) { List<object> rows = []; await foreach (var x in db.Positions.AsNoTracking().Where(x => string.IsNullOrWhiteSpace(search) || EF.Functions.ILike(x.Code, "%" + search + "%") || EF.Functions.ILike(x.Title, "%" + search + "%")).OrderBy(x => x.Title).Take(Math.Clamp(limit, 1, 100)).AsAsyncEnumerable().WithCancellation(ct)) rows.Add(new { id = x.Id, code = x.Code, title = x.Title }); return Result.Success<IReadOnlyList<object>>(rows); }

    private async Task<object?> ReadVersion(DbConnection c, Guid definition, Guid id, CancellationToken ct) { await using DbCommand v = Cmd(c, "select version_number,status,created_at_utc,version from barang.workflow_definition_versions where company_id=@company and definition_id=@definition and id=@id", ("company", Company), ("definition", definition), ("id", id)); await using DbDataReader r = await v.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) return null; int number = r.GetInt32(0); string status = r.GetString(1); DateTimeOffset created = r.GetFieldValue<DateTimeOffset>(2); long version = r.GetInt64(3); await r.DisposeAsync(); List<object> nodes = []; await using DbCommand n = Cmd(c, "select id,node_key,type,title_json,description_json,position_id,x,y,version from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version order by y,x,node_key", ("company", Company), ("version", id)); await using DbDataReader nr = await n.ExecuteReaderAsync(ct); while (await nr.ReadAsync(ct)) { using JsonDocument title = JsonDocument.Parse(nr.GetString(3)); using JsonDocument desc = JsonDocument.Parse(nr.GetString(4)); nodes.Add(new { id = nr.GetGuid(0), key = nr.GetString(1), type = nr.GetString(2), title = title.RootElement.Clone(), description = desc.RootElement.Clone(), positionId = nr.IsDBNull(5) ? (Guid?)null : nr.GetGuid(5), x = nr.GetDecimal(6), y = nr.GetDecimal(7), version = nr.GetInt64(8) }); } await nr.DisposeAsync(); List<object> transitions = []; await using DbCommand t = Cmd(c, "select tr.id,f.node_key,to_node.node_key,condition_json,is_default,sort_order,tr.version from barang.workflow_definition_transitions tr join barang.workflow_definition_nodes f on f.id=tr.from_node_id join barang.workflow_definition_nodes to_node on to_node.id=tr.to_node_id where tr.company_id=@company and tr.definition_version_id=@version order by sort_order,tr.id", ("company", Company), ("version", id)); await using DbDataReader tr = await t.ExecuteReaderAsync(ct); while (await tr.ReadAsync(ct)) { using JsonDocument condition = JsonDocument.Parse(tr.GetString(3)); JsonElement cnd = condition.RootElement; JsonElement? op = cnd.TryGetProperty("operator", out var opValue) ? opValue.Clone() : null; JsonElement? title = cnd.TryGetProperty("title", out var titleValue) ? titleValue.Clone() : null; transitions.Add(new { id = tr.GetGuid(0), fromKey = tr.GetString(1), toKey = tr.GetString(2), fieldCode = StringValue(cnd, "fieldCode"), @operator = op, value = StringValue(cnd, "value"), isDefault = tr.GetBoolean(4), sortOrder = tr.GetInt32(5), outcomeCode = StringValue(cnd, "outcome"), sourceAnchor = StringValue(cnd, "sourceAnchor") ?? "Auto", targetAnchor = StringValue(cnd, "targetAnchor") ?? "Auto", title, version = tr.GetInt64(6) }); } return new { id, number, status, createdAtUtc = created, isReadOnly = status != "Draft", nodes, transitions, version }; }
    private static string? StringValue(JsonElement x, string p) => x.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static string TypeName(int type) => type switch { 0 => "Start", 1 => "End", 2 => "UserTask", 3 => "ExclusiveGateway", 4 => "ParallelGateway", _ => "UserTask" };
    private static List<object> Validate(WorkflowGraphCommand c) { List<object> errors = []; if (c.Nodes.Count(x => x.Type == 0) != 1) errors.Add(new { nodeKey = "", messageKey = "ExactlyOneStart" }); if (!c.Nodes.Any(x => x.Type == 1)) errors.Add(new { nodeKey = "", messageKey = "AtLeastOneEnd" }); HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase); foreach (WorkflowGraphNode n in c.Nodes) { if (!keys.Add(n.Key.Trim())) errors.Add(new { nodeKey = n.Key, messageKey = "DuplicateNodeKey" }); if (n.Type == 2 && n.PositionId is null && !n.Key.Contains("InventoryIssue", StringComparison.OrdinalIgnoreCase)) errors.Add(new { nodeKey = n.Key, messageKey = "TaskPositionRequired" }); } foreach (WorkflowGraphTransition e in c.Transitions) if (!keys.Contains(e.FromKey) || !keys.Contains(e.ToKey)) errors.Add(new { nodeKey = e.FromKey, messageKey = "DanglingTransition" }); return errors; }
    private async Task SetCompany(DbConnection c, CancellationToken ct) { await using DbCommand q = c.CreateCommand(); q.CommandText = "select set_config('barang.company_id',cast(@company as text),false)"; DbParameter p = q.CreateParameter(); p.ParameterName = "company"; p.Value = Company; q.Parameters.Add(p); await q.ExecuteScalarAsync(ct); }
    private static DbCommand Cmd(DbConnection c, string sql, params (string Name, object? Value)[] values) { DbCommand q = c.CreateCommand(); q.CommandText = sql; foreach (var (name, value) in values) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; q.Parameters.Add(p); } return q; }
    private static DbCommand CmdTx(DbConnection c, string sql, params object[] values)
    {
        DbTransaction? transaction = values.OfType<DbTransaction>().FirstOrDefault();
        DbCommand q = c.CreateCommand(); q.CommandText = sql; q.Transaction = transaction;
        foreach (object value in values)
        {
            if (value is DbTransaction) continue;
            Type type = value.GetType();
            if (!type.IsValueType || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ValueTuple<,>)) continue;
            System.Reflection.FieldInfo[] fields = type.GetFields();
            string name = Convert.ToString(fields[0].GetValue(value), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            object? parameterValue = fields[1].GetValue(value);
            DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = parameterValue ?? DBNull.Value; q.Parameters.Add(p);
        }
        return q;
    }
}
