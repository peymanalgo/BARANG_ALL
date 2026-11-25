using System.Data;
using System.Data.Common;
using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Auditing;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Workflow;

/// <summary>BPMS professionalization queries + version rollback over the existing
/// workflow engine tables (read-only SQL + one admin copy operation). No schema changes.</summary>
public sealed class WorkflowBpmsAdministration(CoreDbContext db, ICompanyContext company, ICurrentActor actor, IAuditWriter audit, TimeProvider clock) : IWorkflowBpmsQueries, IWorkflowBpmsAdministration
{
    private Guid Company => company.CompanyId is { } id && id != Guid.Empty ? id : throw new InvalidOperationException("Company context is required.");
    private Guid Actor => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : throw new InvalidOperationException("Actor context is required.");
    private DateTimeOffset Now => clock.GetUtcNow();

    // ── monitoring ───────────────────────────────────────────────────────────
    public async Task<Result<object>> MonitoringSummaryAsync(CancellationToken ct)
    {
        DbConnection c = await Open(ct);
        object instanceKpis; object taskKpis; List<object> versions; List<object> dwell; List<object> waiting;
        await using (DbCommand q = Cmd(c, """
            select count(*) filter(where status='Running'),count(*) filter(where status='Completed'),count(*) filter(where status='Suspended'),
                   count(*) filter(where status='Cancelled'),count(*) filter(where status='Failed'),
                   coalesce(round(avg(extract(epoch from (completed_at_utc-started_at_utc))/3600.0)::numeric,1),0),
                   min(started_at_utc)
            from barang.workflow_instances where company_id=@company
            """, ("company", Company)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            instanceKpis = new { running = r.GetInt64(0), completed = r.GetInt64(1), suspended = r.GetInt64(2), cancelled = r.GetInt64(3), failed = r.GetInt64(4), avgCycleHours = r.GetDecimal(5), oldestStart = r.GetFieldValue<DateTimeOffset>(6) };
        }
        await using (DbCommand q = Cmd(c, """
            select count(*) filter(where status='Open'),count(*) filter(where status='Claimed'),
                   count(*) filter(where status in('Open','Claimed') and created_at_utc<@attention),
                   count(*) filter(where status in('Open','Claimed') and created_at_utc<@long_wait),
                   coalesce(round(avg(extract(epoch from (completed_at_utc-created_at_utc))/3600.0)::numeric,1),0)
            from barang.workflow_runtime_tasks where company_id=@company
            """, ("company", Company), ("attention", Now.AddHours(-8)), ("long_wait", Now.AddHours(-24))))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        {
            await r.ReadAsync(ct);
            taskKpis = new { open = r.GetInt64(0), claimed = r.GetInt64(1), attention = r.GetInt64(2), overdue = r.GetInt64(3), avgTaskHours = r.GetDecimal(4) };
        }
        versions = [];
        await using (DbCommand q = Cmd(c, """
            select v.id,v.version_number,v.status,v.created_at_utc,
                   count(i.id) filter(where i.status='Running'),count(i.id) filter(where i.status='Completed'),
                   coalesce(round(avg(extract(epoch from (i.completed_at_utc-i.started_at_utc))/3600.0)::numeric,1),0)
            from barang.workflow_definition_versions v
            left join barang.workflow_instances i on i.company_id=v.company_id and i."DefinitionVersionId"=v.id
            where v.company_id=@company group by v.id order by v.version_number desc limit 12
            """, ("company", Company)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
                versions.Add(new { id = r.GetGuid(0), number = r.GetInt32(1), status = r.GetString(2), createdAtUtc = r.GetFieldValue<DateTimeOffset>(3), running = r.GetInt64(4), completed = r.GetInt64(5), avgCycleHours = r.GetDecimal(6) });
        dwell = [];
        await using (DbCommand q = Cmd(c, """
            select t.node_key,coalesce(n.title_json->>'fa',t.node_key),count(*),
                   coalesce(round(avg(extract(epoch from (t.completed_at_utc-t.created_at_utc))/3600.0)::numeric,1),0),
                   coalesce(round(max(extract(epoch from (t.completed_at_utc-t.created_at_utc))/3600.0)::numeric,1),0)
            from barang.workflow_runtime_tasks t
            left join barang.workflow_definition_nodes n on n.company_id=t.company_id and n.id=t.definition_node_id
            where t.company_id=@company and t.status='Completed'
            group by 1,2 having count(*)>=1 order by 4 desc limit 8
            """, ("company", Company)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
                dwell.Add(new { nodeKey = r.GetString(0), title = r.GetString(1), count = r.GetInt64(2), avgHours = r.GetDecimal(3), maxHours = r.GetDecimal(4) });
        waiting = [];
        await using (DbCommand q = Cmd(c, """
            select t.node_key,coalesce(n.title_json->>'fa',t.node_key),count(*),
                   coalesce(round(avg(extract(epoch from (@now-t.created_at_utc))/3600.0)::numeric,1),0),
                   coalesce(round(max(extract(epoch from (@now-t.created_at_utc))/3600.0)::numeric,1),0)
            from barang.workflow_runtime_tasks t
            left join barang.workflow_definition_nodes n on n.company_id=t.company_id and n.id=t.definition_node_id
            where t.company_id=@company and t.status in('Open','Claimed')
            group by 1,2 order by 4 desc limit 8
            """, ("company", Company), ("now", Now)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
                waiting.Add(new { nodeKey = r.GetString(0), title = r.GetString(1), count = r.GetInt64(2), avgHours = r.GetDecimal(3), maxHours = r.GetDecimal(4) });
        await c.CloseAsync();
        return Result.Success<object>(new { generatedAtUtc = Now, instances = instanceKpis, tasks = taskKpis, versions, bottleneckDwell = dwell, bottleneckWaiting = waiting });
    }

    // ── instances list ──────────────────────────────────────────────────────
    public async Task<Result<object>> InstancesAsync(string? status, int page, int pageSize, CancellationToken ct)
    {
        DbConnection c = await Open(ct);
        string st = (status ?? "").Trim();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        List<object> rows = []; long total = 0;
        await using (DbCommand q = Cmd(c, """
            select i.id,i.status,i.started_at_utc,i.completed_at_utc,o.order_number,coalesce(c.display_name,''),
                   coalesce((select string_agg(distinct t.node_key,', ') from barang.workflow_runtime_tasks t where t.company_id=i.company_id and t.workflow_instance_id=i.id and t.status in('Open','Claimed')),''),
                   coalesce(round(extract(epoch from (coalesce(i.completed_at_utc,@now)-i.started_at_utc))/3600.0::numeric,1),0),
                   (select count(*) from barang.workflow_runtime_tasks t where t.company_id=i.company_id and t.workflow_instance_id=i.id and t.status in('Open','Claimed') and t.created_at_utc<@long_wait),
                   count(*) over()
            from barang.workflow_instances i
            join barang.orders o on o.company_id=i.company_id and o.id=i.order_id
            left join barang.customers c on c.company_id=o.company_id and c.id=o.customer_id
            where i.company_id=@company and (@status='' or i.status=@status)
            order by i.started_at_utc desc,i.id offset @skip limit @take
            """, ("company", Company), ("status", st), ("now", Now), ("long_wait", Now.AddHours(-24)), ("skip", (page - 1) * pageSize), ("take", pageSize)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
            {
                total = r.GetInt64(9);
                rows.Add(new { id = r.GetGuid(0), status = r.GetString(1), startedAtUtc = r.GetFieldValue<DateTimeOffset>(2), completedAtUtc = r.IsDBNull(3) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(3), orderNumber = r.IsDBNull(4) ? null : r.GetString(4), customer = r.GetString(5), currentNodes = r.GetString(6), cycleHours = r.GetDecimal(7), overdueTasks = r.GetInt64(8) });
            }
        await c.CloseAsync();
        return Result.Success<object>(new { items = rows, page, pageSize, total, status = st });
    }

    // ── instance timeline ───────────────────────────────────────────────────
    public async Task<Result<object>> InstanceTimelineAsync(Guid id, CancellationToken ct)
    {
        DbConnection c = await Open(ct);
        object? header = null;
        await using (DbCommand q = Cmd(c, """
            select i.id,i.status,i.current_stage,i.started_at_utc,i.completed_at_utc,o.order_number,coalesce(c.display_name,''),v.version_number,
                   coalesce(round(extract(epoch from (coalesce(i.completed_at_utc,@now)-i.started_at_utc))/3600.0::numeric,1),0)
            from barang.workflow_instances i
            join barang.orders o on o.company_id=i.company_id and o.id=i.order_id
            left join barang.customers c on c.company_id=o.company_id and c.id=o.customer_id
            left join barang.workflow_definition_versions v on v.company_id=i.company_id and v.id=i."DefinitionVersionId"
            where i.company_id=@company and i.id=@id
            """, ("company", Company), ("id", id), ("now", Now)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            if (await r.ReadAsync(ct))
                header = new { id = r.GetGuid(0), status = r.GetString(1), currentStage = r.GetString(2), startedAtUtc = r.GetFieldValue<DateTimeOffset>(3), completedAtUtc = r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4), orderNumber = r.IsDBNull(5) ? null : r.GetString(5), customer = r.GetString(6), definitionVersion = r.IsDBNull(7) ? (int?)null : r.GetInt32(7), cycleHours = r.GetDecimal(8) };
        if (header is null) { await c.CloseAsync(); return Result.Failure<object>(Error.NotFound("workflow.instance.not_found")); }
        List<object> tasks = [];
        await using (DbCommand q = Cmd(c, """
            select t.id,t.node_key,coalesce(n.title_json->>'fa',t.node_key),coalesce(n.title_json->>'en',t.node_key),t.status,t.created_at_utc,t.claimed_at_utc,t.completed_at_utc,
                   coalesce(cp.display_name,''),coalesce(pp.display_name,''),coalesce(t.result_json->>'outcome',''),coalesce(pos.title,''),
                   coalesce(round(extract(epoch from (coalesce(t.completed_at_utc,@now)-t.created_at_utc))/3600.0::numeric,1),0),
                   (t.status in('Open','Claimed') and t.created_at_utc<@long_wait)
            from barang.workflow_runtime_tasks t
            left join barang.workflow_definition_nodes n on n.company_id=t.company_id and n.id=t.definition_node_id
            left join barang.application_users cu on cu.id=t.claimed_by_user_id left join barang.persons cp on cp.id=cu.person_id
            left join barang.application_users pu on pu.id=t.completed_by_user_id left join barang.persons pp on pp.id=pu.person_id
            left join barang.positions pos on pos.company_id=t.company_id and pos.id=t.position_id
            where t.company_id=@company and t.workflow_instance_id=@id order by t.created_at_utc,t.id
            """, ("company", Company), ("id", id), ("now", Now), ("long_wait", Now.AddHours(-24))))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
                tasks.Add(new { id = r.GetGuid(0), nodeKey = r.GetString(1), titleFa = r.GetString(2), titleEn = r.GetString(3), status = r.GetString(4), createdAtUtc = r.GetFieldValue<DateTimeOffset>(5), claimedAtUtc = r.IsDBNull(6) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(6), completedAtUtc = r.IsDBNull(7) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(7), claimedBy = r.GetString(8), completedBy = r.GetString(9), outcome = r.GetString(10), position = r.GetString(11), durationHours = r.GetDecimal(12), overdue = r.GetBoolean(13) });
        List<object> events = [];
        await using (DbCommand q = Cmd(c, """
            select h.event_type,h.occurred_at_utc,coalesce(p.display_name,''),h.summary
            from barang.workflow_history h
            left join barang.application_users u on u.id=h.actor_user_id left join barang.persons p on p.id=u.person_id
            where h.company_id=@company and h.workflow_instance_id=@id order by h.occurred_at_utc,h.id
            """, ("company", Company), ("id", id)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct))
                events.Add(new { eventType = r.GetString(0), occurredAtUtc = r.GetFieldValue<DateTimeOffset>(1), actor = r.GetString(2), summary = r.IsDBNull(3) ? null : r.GetString(3) });
        await c.CloseAsync();
        return Result.Success<object>(new { instance = header, tasks, events });
    }

    // ── version diff ────────────────────────────────────────────────────────
    public async Task<Result<object>> VersionDiffAsync(Guid fromVersionId, Guid toVersionId, CancellationToken ct)
    {
        DbConnection c = await Open(ct);
        Dictionary<Guid, int> numbers = [];
        await using (DbCommand q = Cmd(c, "select id,version_number from barang.workflow_definition_versions where company_id=@company and id in (@from,@to)", ("company", Company), ("from", fromVersionId), ("to", toVersionId)))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
            while (await r.ReadAsync(ct)) numbers[r.GetGuid(0)] = r.GetInt32(1);
        if (!numbers.TryGetValue(fromVersionId, out _) || !numbers.TryGetValue(toVersionId, out _)) { await c.CloseAsync(); return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found")); }
        Dictionary<string, NodeSnapshot> fromNodes = await ReadNodes(c, fromVersionId, ct), toNodes = await ReadNodes(c, toVersionId, ct);
        List<(string From, string To, string Condition)> fromEdges = await ReadEdges(c, fromVersionId, ct), toEdges = await ReadEdges(c, toVersionId, ct);
        await c.CloseAsync();
        HashSet<string> fromKeys = [.. fromNodes.Keys], toKeys = [.. toNodes.Keys];
        List<object> added = toKeys.Except(fromKeys).OrderBy(x => x, StringComparer.Ordinal).Select(k => NodeChange(toNodes[k])).ToList();
        List<object> removed = fromKeys.Except(toKeys).OrderBy(x => x, StringComparer.Ordinal).Select(k => NodeChange(fromNodes[k])).ToList();
        List<object> changed = [];
        foreach (string key in fromKeys.Intersect(toKeys).OrderBy(x => x, StringComparer.Ordinal))
        {
            NodeSnapshot a = fromNodes[key], b = toNodes[key];
            List<string> changes = [];
            if (a.Type != b.Type) changes.Add("type");
            if (a.PositionId != b.PositionId) changes.Add("position");
            if (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) > 20) changes.Add("moved");
            if (!string.Equals(a.TitleFa, b.TitleFa, StringComparison.Ordinal) || !string.Equals(a.TitleEn, b.TitleEn, StringComparison.Ordinal)) changes.Add("title");
            if (changes.Count > 0) changed.Add(new { key, changes });
        }
        HashSet<string> fe = [.. fromEdges.Select(x => x.From + "→" + x.To)], te = [.. toEdges.Select(x => x.From + "→" + x.To)];
        return Result.Success<object>(new
        {
            from = new { id = fromVersionId, number = numbers[fromVersionId] },
            to = new { id = toVersionId, number = numbers[toVersionId] },
            nodes = new { added, removed, changed, fromCount = fromKeys.Count, toCount = toKeys.Count },
            edges = new { added = te.Except(fe).OrderBy(x => x, StringComparer.Ordinal).ToList(), removed = fe.Except(te).OrderBy(x => x, StringComparer.Ordinal).ToList(), fromCount = fe.Count, toCount = te.Count },
        });
    }

    // ── version rollback (restore old version into a draft) ─────────────────
    public async Task<Result<object>> RestoreVersionDraftAsync(Guid versionId, CancellationToken ct)
    {
        await using DbConnection c = db.Database.GetDbConnection();
        await c.OpenAsync(ct); await SetCompany(c, ct);
        await using DbTransaction tx = await c.BeginTransactionAsync(ct);
        Guid definition; int sourceNumber; string status;
        await using (DbCommand q = CmdTx(c, "select definition_id,version_number,status from barang.workflow_definition_versions where company_id=@company and id=@id for update", ("company", Company), ("id", versionId), tx))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        { if (!await r.ReadAsync(ct)) { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.NotFound("workflow.definition.version_not_found")); } definition = r.GetGuid(0); sourceNumber = r.GetInt32(1); status = r.GetString(2); }
        if (status == "Draft") { await tx.RollbackAsync(ct); return Result.Failure<object>(Error.BusinessRule("workflow.definition.restore_source_not_published")); }
        Guid target; int targetNumber;
        object? draftId;
        await using (DbCommand d = CmdTx(c, "select id from barang.workflow_definition_versions where company_id=@company and definition_id=@definition and status='Draft' order by version_number desc limit 1 for update", ("company", Company), ("definition", definition), tx))
            draftId = await d.ExecuteScalarAsync(ct);
        if (draftId is Guid existingDraft)
        {
            target = existingDraft;
            await using (DbCommand n = CmdTx(c, "select version_number from barang.workflow_definition_versions where company_id=@company and id=@id", ("company", Company), ("id", target), tx)) targetNumber = Convert.ToInt32(await n.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
            await using (DbCommand bump = CmdTx(c, "update barang.workflow_definition_versions set change_note_json=@note::jsonb,version=version+1,created_by_user_id=@actor where company_id=@company and id=@id", ("note", JsonSerializer.Serialize(new { fa = $"بازگردانی از نسخه {sourceNumber}", en = $"Restored from version {sourceNumber}", ar = $"استعادة من النسخة {sourceNumber}" })), ("actor", Actor), ("company", Company), ("id", target), tx)) await bump.ExecuteNonQueryAsync(ct);
        }
        else
        {
            target = Guid.NewGuid();
            await using (DbCommand next = CmdTx(c, "select coalesce(max(version_number),0)+1 from barang.workflow_definition_versions where company_id=@company and definition_id=@definition", ("company", Company), ("definition", definition), tx)) targetNumber = Convert.ToInt32(await next.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
            await using (DbCommand ins = CmdTx(c, "insert into barang.workflow_definition_versions(id,company_id,definition_id,version_number,status,change_note_json,created_at_utc,created_by_user_id,version) values(@id,@company,@definition,@number,'Draft',@note::jsonb,@now,@actor,1)",
                ("id", target), ("company", Company), ("definition", definition), ("number", targetNumber), ("note", JsonSerializer.Serialize(new { fa = $"بازگردانی از نسخه {sourceNumber}", en = $"Restored from version {sourceNumber}", ar = $"استعادة من النسخة {sourceNumber}" })), ("now", Now), ("actor", Actor), tx)) await ins.ExecuteNonQueryAsync(ct);
        }
        await using (DbCommand delT = CmdTx(c, "delete from barang.workflow_definition_transitions where company_id=@company and definition_version_id=@target", ("company", Company), ("target", target), tx)) await delT.ExecuteNonQueryAsync(ct);
        await using (DbCommand delN = CmdTx(c, "delete from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@target", ("company", Company), ("target", target), tx)) await delN.ExecuteNonQueryAsync(ct);
        await using (DbCommand cpN = CmdTx(c, """
            insert into barang.workflow_definition_nodes(id,company_id,definition_version_id,node_key,type,title_json,description_json,position_id,x,y,version)
            select gen_random_uuid(),@company,@target,node_key,type,title_json,description_json,position_id,x,y,1
            from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@source
            """, ("company", Company), ("target", target), ("source", versionId), tx)) await cpN.ExecuteNonQueryAsync(ct);
        await using (DbCommand cpE = CmdTx(c, """
            insert into barang.workflow_definition_transitions(id,company_id,definition_version_id,from_node_id,to_node_id,condition_json,is_default,sort_order,version)
            select gen_random_uuid(),@company,@target,tf.id,tt.id,tr.condition_json,tr.is_default,tr.sort_order,1
            from barang.workflow_definition_transitions tr
            join barang.workflow_definition_nodes sf on sf.company_id=tr.company_id and sf.definition_version_id=@source and sf.id=tr.from_node_id
            join barang.workflow_definition_nodes tf on tf.company_id=@company and tf.definition_version_id=@target and tf.node_key=sf.node_key
            join barang.workflow_definition_nodes st on st.company_id=tr.company_id and st.definition_version_id=@source and st.id=tr.to_node_id
            join barang.workflow_definition_nodes tt on tt.company_id=@company and tt.definition_version_id=@target and tt.node_key=st.node_key
            where tr.company_id=@company and tr.definition_version_id=@source
            """, ("company", Company), ("target", target), ("source", versionId), tx)) await cpE.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        audit.Write(new("workflow.definition.restored", "workflow", target.ToString(), ChangeKind: AuditChangeKind.Modified, Metadata: new Dictionary<string, object?> { ["sourceVersion"] = sourceNumber, ["draftVersion"] = targetNumber }));
        await c.CloseAsync();
        return Result.Success<object>(new { id = target, number = targetNumber, sourceNumber, status = "Draft" });
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    private async Task<DbConnection> Open(CancellationToken ct)
    { DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct); return c; }
    private sealed record NodeSnapshot(string Key, string Type, Guid? PositionId, decimal X, decimal Y, string TitleFa, string TitleEn);
    private static object NodeChange(NodeSnapshot n) => new { key = n.Key, type = n.Type, titleFa = n.TitleFa, positionId = n.PositionId };
    private async Task<Dictionary<string, NodeSnapshot>> ReadNodes(DbConnection c, Guid version, CancellationToken ct)
    {
        Dictionary<string, NodeSnapshot> map = new(StringComparer.OrdinalIgnoreCase);
        await using DbCommand q = Cmd(c, "select node_key,type,position_id,x,y,title_json from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version", ("company", Company), ("version", version));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            using JsonDocument title = JsonDocument.Parse(r.GetString(5));
            map[r.GetString(0)] = new NodeSnapshot(r.GetString(0), r.GetString(1), r.IsDBNull(2) ? null : r.GetGuid(2), r.GetDecimal(3), r.GetDecimal(4), Txt(title.RootElement, "fa"), Txt(title.RootElement, "en"));
        }
        return map;
    }
    private async Task<List<(string From, string To, string Condition)>> ReadEdges(DbConnection c, Guid version, CancellationToken ct)
    {
        List<(string From, string To, string Condition)> list = [];
        await using DbCommand q = Cmd(c, "select f.node_key,t.node_key,tr.condition_json from barang.workflow_definition_transitions tr join barang.workflow_definition_nodes f on f.id=tr.from_node_id join barang.workflow_definition_nodes t on t.id=tr.to_node_id where tr.company_id=@company and tr.definition_version_id=@version order by tr.sort_order,tr.id", ("company", Company), ("version", version));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) list.Add((r.GetString(0), r.GetString(1), r.GetString(2)));
        return list;
    }
    private static string Txt(JsonElement root, string key) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private async Task SetCompany(DbConnection c, CancellationToken ct) { await using DbCommand q = c.CreateCommand(); q.CommandText = "select set_config('barang.company_id',cast(@company as text),false)"; DbParameter p = q.CreateParameter(); p.ParameterName = "company"; p.Value = Company; q.Parameters.Add(p); await q.ExecuteScalarAsync(ct); }
    private static DbCommand Cmd(DbConnection c, string sql, params (string Name, object? Value)[] values) { DbCommand q = c.CreateCommand(); q.CommandText = sql; foreach (var (name, value) in values) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; q.Parameters.Add(p); } return q; }
    private static DbCommand CmdTx(DbConnection c, string sql, params object?[] values)
    {
        DbTransaction? transaction = values.OfType<DbTransaction>().FirstOrDefault();
        DbCommand q = c.CreateCommand(); q.CommandText = sql; q.Transaction = transaction;
        foreach (object? value in values)
        {
            if (value is DbTransaction) continue;
            if (value is System.Runtime.CompilerServices.ITuple item && item.Length == 2 && item[0] is string name) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = item[1] ?? DBNull.Value; q.Parameters.Add(p); }
        }
        return q;
    }
}
