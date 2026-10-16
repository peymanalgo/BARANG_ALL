using System.Data;
using System.Data.Common;
using System.Text.Json;
using BARANG.Core.Application.Abstractions.Actors;
using BARANG.Core.Application.Abstractions.Companies;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Files;
using BARANG.Core.Application.Workflow;
using BARANG.Core.Application.Production;
using BARANG.Core.Application.Inventory;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Production;
using BARANG.Core.Infrastructure.Persistence;
using BARANG.Core.Infrastructure.Inventory;
using BARANG.Core.Infrastructure.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BARANG.Core.Infrastructure.Workflow;

public sealed class WorkflowDefinitionRuntime(CoreDbContext db, ITransactionExecutor tx, ICompanyContext company, ICurrentActor actor, IArtworkFileAdministration files, InventoryAdministration inventory, IProductionAdministration production, TimeProvider clock) : IWorkflowDefinitionRuntime
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private Guid Company => company.CompanyId is { } id && id != Guid.Empty ? id : throw new InvalidOperationException("Company context is required.");
    private Guid Actor => Guid.TryParse(actor.Actor.UserId, out Guid id) ? id : throw new InvalidOperationException("Actor context is required.");
    private DateTimeOffset Now => clock.GetUtcNow();
    private static readonly object[] Empty = [];
    private static readonly string[] Outcomes = ["Approve", "Reject"];
    private static bool IsFinancialApprovalNode(string nodeKey) =>
        nodeKey.Contains("FinancialApproval", StringComparison.OrdinalIgnoreCase)
        || nodeKey.Equals("FINANCE", StringComparison.OrdinalIgnoreCase)
        || nodeKey.Contains("Finance", StringComparison.OrdinalIgnoreCase);
    private static bool IsProductionPlanningNode(string nodeKey) => nodeKey.Contains("ProductionPlanning", StringComparison.OrdinalIgnoreCase) || nodeKey.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase);
    private static bool IsDesignNode(string nodeKey) => nodeKey.Contains("DesignPreparation", StringComparison.OrdinalIgnoreCase) || nodeKey.Contains("DESIGN", StringComparison.OrdinalIgnoreCase);
    private static bool IsPrintNode(string nodeKey) => nodeKey.Contains("Printing", StringComparison.OrdinalIgnoreCase) || nodeKey.Contains("PRINT", StringComparison.OrdinalIgnoreCase);
    private static bool IsHeatPressNode(string nodeKey) => nodeKey.Contains("HeatPress", StringComparison.OrdinalIgnoreCase) || nodeKey.Contains("PRESS", StringComparison.OrdinalIgnoreCase);

    public Task<Result<object>> StartAsync(StartDefinitionWorkflowCommand command, CancellationToken ct) => tx.ExecuteAsync(t => StartInsideTransactionAsync(command.OrderId, t), ct);
    public async Task<Result<object>> StartInsideTransactionAsync(Guid orderId, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct); DbTransaction? transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        await using DbCommand existing = CmdTx(c, "select id from barang.workflow_instances where company_id=@company and order_id=@order_id", ("company", Company), ("order_id", orderId), transaction); object? found = await existing.ExecuteScalarAsync(ct); if (found is Guid old) return Result.Success<object>(new { id = old, orderId, idempotent = true });
        Guid definition; Guid version; bool hasDefinition; await using (DbCommand q = CmdTx(c, "select d.id,d.active_version_id from barang.workflow_definitions d where d.company_id=@company and d.code='BARANG.ORDER'", ("company", Company), transaction)) await using (DbDataReader r = await q.ExecuteReaderAsync(ct)) { hasDefinition = await r.ReadAsync(ct); definition = hasDefinition ? r.GetGuid(0) : Guid.Empty; version = hasDefinition && !r.IsDBNull(1) ? r.GetGuid(1) : Guid.Empty; }
        if (!hasDefinition || version == Guid.Empty) return Result.Failure<object>(Error.BusinessRule("workflow.definition.no_published_version"));
        Guid instance = Guid.NewGuid(); await using (DbCommand ins = CmdTx(c, "insert into barang.workflow_instances(id,company_id,order_id,definition_code,provider_instance_id,current_stage,status,started_at_utc,updated_at_utc,version,\"DefinitionVersionId\") values(@id,@company,@order,'BARANG.ORDER',@provider,'AdministrativeReview','Running',@now,@now,1,@version)", ("id", instance), ("company", Company), ("order", orderId), ("provider", instance.ToString()), ("now", Now), ("version", version), transaction)) await ins.ExecuteNonQueryAsync(ct);
        await using DbCommand start = CmdTx(c, "select id,node_key,position_id from barang.workflow_definition_nodes where company_id=@company and definition_version_id=@version and type='Start'", ("company", Company), ("version", version), transaction); await using DbDataReader sr = await start.ExecuteReaderAsync(ct); if (!await sr.ReadAsync(ct)) return Result.Failure<object>(Error.BusinessRule("workflow.definition.start_missing")); Guid startNode = sr.GetGuid(0); await sr.DisposeAsync();
        await CreateNextTasks(c, transaction, instance, version, startNode, ct); await History(c, transaction, instance, null, "InstanceStarted", null, ct); return Result.Success<object>(new { id = instance, orderId, definitionVersionId = version });
    }

    public Task<Result<object>> ClaimTaskAsync(Guid id, long version, CancellationToken ct) => tx.ExecuteAsync(async t => { DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(t); await SetCompany(c, t); DbTransaction? tr = db.Database.CurrentTransaction?.GetDbTransaction(); await using DbCommand q = CmdTx(c, "update barang.workflow_runtime_tasks t set status='Claimed',claimed_at_utc=@now,claimed_by_user_id=@actor,version=t.version+1 where t.company_id=@company and t.id=@id and t.version=@version and t.status in ('Open','Claimed') and (t.claimed_by_user_id is null or t.claimed_by_user_id=@actor) and (t.node_key not ilike '%InventoryIssue%' or not exists(select 1 from barang.workflow_instances wi join barang.order_items oi on oi.company_id=wi.company_id and oi.order_id=wi.order_id and oi.is_active where wi.company_id=@company and wi.id=t.workflow_instance_id and not exists(select 1 from barang.stock_reservations r join barang.warehouse_responsibilities wr on wr.company_id=r.company_id and wr.warehouse_id=r.warehouse_id join barang.application_users au on au.id=wr.application_user_id join barang.user_companies uc on uc.user_id=au.id and uc.company_id=wr.company_id where r.company_id=@company and r.order_item_id=oi.id and r.status in ('Active','PartiallyReleased') and r.remaining_quantity>0 and wr.application_user_id=@actor and wr.is_active and au.account_state='Active' and uc.valid_from_utc<=@now and (uc.valid_to_utc is null or uc.valid_to_utc>@now)))) returning t.version", ("now", Now), ("actor", Actor), ("company", Company), ("id", id), ("version", version), tr); object? value = await q.ExecuteScalarAsync(t); return value is null ? Result.Failure<object>(Error.BusinessRule("inventory.issue.warehouse_responsible_not_configured")) : Result.Success<object>(new { id, claimedByUserId = Actor, version = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) }); }, ct);
    public Task<Result<object>> ReleaseTaskAsync(Guid id, long version, CancellationToken ct) => tx.ExecuteAsync(async t => { DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(t); await SetCompany(c, t); DbTransaction? tr = db.Database.CurrentTransaction?.GetDbTransaction(); await using DbCommand q = CmdTx(c, "update barang.workflow_runtime_tasks set status='Open',claimed_at_utc=null,claimed_by_user_id=null,claimed_on_behalf_of_user_id=null,version=version+1 where company_id=@company and id=@id and version=@version and claimed_by_user_id=@actor returning version", ("company", Company), ("id", id), ("version", version), ("actor", Actor), tr); object? value = await q.ExecuteScalarAsync(t); return value is null ? Result.Failure<object>(Error.BusinessRule("workflow.task.not_owner")) : Result.Success<object>(new { id, status = "Open", version = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) }); }, ct);
    public Task<Result<object>> CompleteTaskAsync(Guid id, string? resultJson, IReadOnlyList<DecisionAttachmentCommand>? attachments, long version, CancellationToken ct) => tx.ExecuteAsync(t => CompleteInside(id, resultJson, attachments, version, t), ct);
    private async Task<Result<object>> CompleteInside(Guid id, string? resultJson, IReadOnlyList<DecisionAttachmentCommand>? attachments, long version, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct); DbTransaction? tr = db.Database.CurrentTransaction?.GetDbTransaction(); Guid instance; Guid node; Guid orderId; string status; Guid? owner; string nodeKey; await using (DbCommand q = CmdTx(c, "select t.workflow_instance_id,t.definition_node_id,t.node_key,t.status,t.claimed_by_user_id,i.order_id from barang.workflow_runtime_tasks t join barang.workflow_instances i on i.id=t.workflow_instance_id and i.company_id=t.company_id where t.company_id=@company and t.id=@id for update", ("company", Company), ("id", id), tr)) await using (DbDataReader r = await q.ExecuteReaderAsync(ct)) { if (!await r.ReadAsync(ct)) return Result.Failure<object>(Error.NotFound("workflow.task.not_found")); instance = r.GetGuid(0); node = r.GetGuid(1); nodeKey = r.GetString(2); status = r.GetString(3); owner = r.IsDBNull(4) ? null : r.GetGuid(4); orderId = r.GetGuid(5); }
        if (status == "Completed") return Result.Success<object>(new { id, status }); if (owner != Actor) return Result.Failure<object>(Error.BusinessRule("workflow.task.not_owner"));
        if (nodeKey.Contains("InventoryIssue", StringComparison.OrdinalIgnoreCase) && !await WarehouseIssueEligible(c, tr, orderId, ct)) return Result.Failure<object>(Error.BusinessRule("inventory.issue.warehouse_responsible_not_configured"));
        JsonDocument? result = null; try { if (!string.IsNullOrWhiteSpace(resultJson)) result = JsonDocument.Parse(resultJson); } catch (JsonException) { return Result.Failure<object>(Error.Validation("workflow.task.invalid_result")); }
        string? outcome = result?.RootElement.TryGetProperty("outcome", out var outcomeValue) == true && outcomeValue.ValueKind == JsonValueKind.String ? outcomeValue.GetString() : null;
        if (IsFinancialApprovalNode(nodeKey))
        {
            await using DbCommand finance = CmdTx(c, "select fs.status,fs.approved_price_snapshot_id,p.id from barang.order_financial_states fs left join barang.order_price_snapshots p on p.company_id=fs.company_id and p.order_id=fs.order_id and p.is_current and p.is_finalized where fs.company_id=@company and fs.order_id=@order_id", ("company", Company), ("order_id", orderId), tr);
            await using DbDataReader financial = await finance.ExecuteReaderAsync(ct);
            if (!await financial.ReadAsync(ct)) return Result.Failure<object>(Error.BusinessRule("finance.approval.required"));
            string statusValue = financial.GetString(0); Guid? approvedSnapshot = financial.IsDBNull(1) ? null : financial.GetGuid(1); Guid? currentSnapshot = financial.IsDBNull(2) ? null : financial.GetGuid(2);
            bool accepted = string.Equals(outcome, "Approve", StringComparison.OrdinalIgnoreCase) && string.Equals(statusValue, "Approved", StringComparison.OrdinalIgnoreCase) && approvedSnapshot is Guid approved && approved == currentSnapshot;
            bool rejected = string.Equals(outcome, "Reject", StringComparison.OrdinalIgnoreCase) && string.Equals(statusValue, "Rejected", StringComparison.OrdinalIgnoreCase);
            if (!accepted && !rejected) return Result.Failure<object>(Error.BusinessRule("finance.approval.required"));
        }
        if (IsProductionPlanningNode(nodeKey))
        {
            Result<IReadOnlyList<ProductionJobView>> generated = await production.GenerateInsideTransactionAsync(new(orderId), ct);
            if (generated.IsFailure) return Result.Failure<object>(generated.Error);
            resultJson = JsonSerializer.Serialize(new { outcome, generated = generated.Value.Select(x => x.Id) });
        }
        else if (IsDesignNode(nodeKey))
        {
            Result gate = await production.ValidateWorkflowCompletionAsync(orderId, ProductionJobType.DesignPreparation, ct);
            if (gate.IsFailure) return Result.Failure<object>(gate.Error);
        }
        else if (IsPrintNode(nodeKey))
        {
            Result gate = await production.ValidateWorkflowCompletionAsync(orderId, ProductionJobType.Print, ct);
            if (gate.IsFailure) return Result.Failure<object>(gate.Error);
        }
        else if (IsHeatPressNode(nodeKey))
        {
            Result gate = await production.ValidateWorkflowCompletionAsync(orderId, ProductionJobType.HeatPress, ct);
            if (gate.IsFailure) return Result.Failure<object>(gate.Error);
        }
        if (nodeKey.Contains("InventoryReservation", StringComparison.OrdinalIgnoreCase))
        {
            if (result is null || !result.RootElement.TryGetProperty("warehouseId", out var warehouse) || warehouse.ValueKind != JsonValueKind.String || !Guid.TryParse(warehouse.GetString(), out Guid warehouseId)) return Result.Failure<object>(Error.Validation("inventory.reservation.warehouse_required"));
            string key = result.RootElement.TryGetProperty("idempotencyKey", out var keyValue) && keyValue.ValueKind == JsonValueKind.String ? keyValue.GetString() ?? $"workflow:{id:N}" : $"workflow:{id:N}";
            Result<ReserveOrderInventoryResult> reserved = await inventory.ReserveOrderInsideTransactionAsync(new(orderId, warehouseId, key), ct);
            if (reserved.IsFailure) return Result.Failure<object>(reserved.Error);
            Result reservationGate = await inventory.ValidateReservationWorkflowCompletionAsync(orderId, warehouseId, reserved.Value, ct);
            if (reservationGate.IsFailure) return Result.Failure<object>(reservationGate.Error);
            resultJson = JsonSerializer.Serialize(new { outcome, warehouseId, idempotencyKey = key, reservations = reserved.Value.Reservations });
        }
        if (nodeKey.Contains("InventoryIssue", StringComparison.OrdinalIgnoreCase))
        {
            if (result is null || !result.RootElement.TryGetProperty("warehouseId", out var issueWarehouse) || issueWarehouse.ValueKind != JsonValueKind.String || !Guid.TryParse(issueWarehouse.GetString(), out Guid warehouseId)) return Result.Failure<object>(Error.Validation("inventory.issue.warehouse_required"));
            string reference = result.RootElement.TryGetProperty("reference", out var referenceValue) && referenceValue.ValueKind == JsonValueKind.String ? referenceValue.GetString() ?? $"WORKFLOW-ISSUE:{id:N}" : $"WORKFLOW-ISSUE:{id:N}";
            List<IssueOrderItemCommand>? items = result.RootElement.TryGetProperty("items", out var issueItems) && issueItems.ValueKind == JsonValueKind.Array ? JsonSerializer.Deserialize<List<IssueOrderItemCommand>>(issueItems.GetRawText(), WebJson) : null;
            if (items is null || items.Count == 0) return Result.Failure<object>(Error.Validation("inventory.issue.items_required"));
            Result<IssueOrderInventoryResult> issued = await inventory.IssueOrderInsideTransactionAsync(new(orderId, warehouseId, reference, items), ct);
            if (issued.IsFailure) return Result.Failure<object>(issued.Error);
            Result issueGate = await inventory.ValidateIssueWorkflowCompletionAsync(orderId, warehouseId, issued.Value, ct);
            if (issueGate.IsFailure) return Result.Failure<object>(issueGate.Error);
            resultJson = JsonSerializer.Serialize(new { outcome, warehouseId, reference, items, reservations = issued.Value.Reservations, transactions = issued.Value.Transactions });
        }
        await using (DbCommand u = CmdTx(c, "update barang.workflow_runtime_tasks set status='Completed',completed_at_utc=@now,completed_by_user_id=@actor,result_json=@result::jsonb,version=version+1 where company_id=@company and id=@id and version=@version", ("now", Now), ("actor", Actor), ("result", resultJson ?? "{}"), ("company", Company), ("id", id), ("version", version), tr)) if (await u.ExecuteNonQueryAsync(ct) != 1) return Result.Failure<object>(Error.Concurrency("workflow.task.concurrency"));
        Guid historyId = await History(c, tr, instance, id, "TaskCompleted", resultJson, ct); await SaveAttachments(c, tr, historyId, instance, id, resultJson, attachments, ct); await CreateNextTasks(c, tr, instance, await VersionForInstance(c, tr, instance, ct), node, ct, outcome); return Result.Success<object>(new { id, status = "Completed" });
    }

    public async Task<Result<object>> EligibleTasksAsync(CartableTaskQuery query, CancellationToken ct) => await TaskPage(query, false, ct);
    public async Task<Result<object>> MyReferralsAsync(CartableTaskQuery query, CancellationToken ct) => await TaskPage(query, true, ct);
        private async Task<Result<object>> TaskPage(CartableTaskQuery q, bool referrals, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection();
        if (c.State != ConnectionState.Open) await c.OpenAsync(ct);
        await SetCompany(c, ct);
        List<string> filters = ["t.company_id=@company", "(t.node_key not ilike '%InventoryIssue%' or not exists(select 1 from barang.order_items oi where oi.company_id=t.company_id and oi.order_id=i.order_id and oi.is_active and not exists(select 1 from barang.stock_reservations r join barang.warehouse_responsibilities wr on wr.company_id=r.company_id and wr.warehouse_id=r.warehouse_id join barang.application_users au on au.id=wr.application_user_id join barang.user_companies uc on uc.user_id=au.id and uc.company_id=wr.company_id where r.company_id=t.company_id and r.order_item_id=oi.id and r.status in ('Active','PartiallyReleased') and r.remaining_quantity>0 and wr.is_active and au.account_state='Active' and uc.valid_from_utc<=@now and (uc.valid_to_utc is null or uc.valid_to_utc>@now) and wr.application_user_id=@actor)) )"];
        List<(string Name, object? Value)> args = [("company", Company), ("actor", Actor), ("now", Now)];
        string eligibility = "(t.claimed_on_behalf_of_user_id=@actor or exists(select 1 from barang.application_users au join barang.position_assignments pa on pa.company_id=t.company_id and pa.person_id=au.person_id and pa.position_id=t.position_id where au.id=@actor and pa.status='Active' and (pa.start_date is null or pa.start_date<=current_date) and (pa.end_date is null or pa.end_date>=current_date)) or exists(select 1 from barang.user_roles ur join barang.roles rr on rr.id=ur.role_id where ur.company_id=t.company_id and ur.user_id=@actor and rr.code='system.administrator' and rr.is_active))";
        filters.Add(referrals ? "t.completed_by_user_id=@actor" : "t.status in ('Open','Claimed') and " + eligibility);
        string search = q.Search?.Trim() ?? "";
        filters.Add("(@search='' or coalesce(o.order_number,'') ilike '%'||@search||'%')"); args.Add(("search", search));
        if (q.TaskId is Guid taskId) { filters.Add("t.id=@task"); args.Add(("task", taskId)); }
        if (!string.IsNullOrWhiteSpace(q.StageKey)) { filters.Add("t.node_key=@stage"); args.Add(("stage", q.StageKey.Trim())); }
        if (q.CustomerId is Guid customerId) { filters.Add("o.customer_id=@customer"); args.Add(("customer", customerId)); }
        if (q.FromDate is DateOnly from) { filters.Add("o.order_date>=@from_date"); args.Add(("from_date", from)); }
        if (q.ToDate is DateOnly to) { filters.Add("o.order_date<=@to_date"); args.Add(("to_date", to)); }
        if (referrals && !string.IsNullOrWhiteSpace(q.Outcome)) { filters.Add("coalesce(t.result_json->>'outcome','')=@outcome"); args.Add(("outcome", q.Outcome.Trim())); }
        if (referrals && !string.IsNullOrWhiteSpace(q.Status)) { filters.Add("o.status=@order_status"); args.Add(("order_status", q.Status.Trim())); }
        if (!string.IsNullOrWhiteSpace(q.Priority)) { filters.Add("coalesce(o.priority,'Normal')=@priority"); args.Add(("priority", q.Priority.Trim())); }
        if (q.StarredOnly) filters.Add("coalesce(ps.is_starred,false)");
        if (!string.IsNullOrWhiteSpace(q.Aging)) { DateTimeOffset attention=Now.AddHours(-CartableAgingPolicy.AttentionHours), longWait=Now.AddHours(-CartableAgingPolicy.LongWaitingHours); args.Add(("attention",attention)); args.Add(("long_wait",longWait)); filters.Add(q.Aging.Trim() switch { "Normal"=>"t.created_at_utc>@attention", "Attention"=>"t.created_at_utc<=@attention and t.created_at_utc>@long_wait", "LongWaiting"=>"t.created_at_utc<=@long_wait", _=>"true" }); }
        if (!string.IsNullOrWhiteSpace(q.FollowUp)) { filters.Add(q.FollowUp.Trim() switch { "HasFollowUp"=>"ps.follow_up_at_utc is not null", "Reached"=>"ps.follow_up_at_utc is not null and ps.follow_up_at_utc<=@now", "Future"=>"ps.follow_up_at_utc>@now", _=>"true" }); args.Add(("now",Now)); }
        string sort=q.Sort?.Trim().ToLowerInvariant() switch { "order"=>"o.order_number", "customer"=>"coalesce(c.display_name,'')", "stage"=>"t.node_key", "priority"=>"coalesce(o.priority,'Normal')", "age"=>"t.created_at_utc", "followup"=>"ps.follow_up_at_utc nulls last", "outcome"=>"coalesce(t.result_json->>'outcome','')", _=>"t.created_at_utc" };
        string direction=q.Descending?"desc":"asc"; int pageSize=Math.Clamp(q.PageSize,1,100); int skip=Math.Max(0,q.Page-1)*pageSize; args.Add(("skip",skip)); args.Add(("take",pageSize));
        string sql=$"""
            select t.id,t.workflow_instance_id,i.order_id,t.node_key,t.status,t.created_at_utc,t.claimed_by_user_id,t.version,o.order_number,o.order_date,o.status,coalesce(c.display_name,''),coalesce(o.priority,'Normal'),coalesce(ps.is_starred,false),ps.seen_at_utc,ps.follow_up_at_utc,count(*) over()
            from barang.workflow_runtime_tasks t join barang.workflow_instances i on i.id=t.workflow_instance_id and i.company_id=t.company_id join barang.orders o on o.id=i.order_id and o.company_id=i.company_id left join barang.customers c on c.id=o.customer_id and c.company_id=o.company_id left join barang.cartable_personal_states ps on ps.company_id=t.company_id and ps.runtime_task_id=t.id and ps.user_id=@actor
            where {string.Join(" and ",filters)} order by {sort} {direction},t.id offset @skip limit @take
            """;
        List<object> rows=[]; long total=0;
        await using DbCommand cmd=Cmd(c,sql,[..args]); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) { total=r.GetInt64(16); rows.Add(new { id=r.GetGuid(0),workflowInstanceId=r.GetGuid(1),orderId=r.GetGuid(2),nodeKey=r.GetString(3),stageKey=r.GetString(3),status=r.GetString(4),createdAtUtc=r.GetFieldValue<DateTimeOffset>(5),claimedByUserId=r.IsDBNull(6)?(Guid?)null:r.GetGuid(6),version=r.GetInt64(7),orderNumber=r.IsDBNull(8)?null:r.GetString(8),orderDate=r.GetFieldValue<DateOnly>(9),effectiveStatusCode=r.GetString(10),customer=r.GetString(11),priority=r.GetString(12),isStarred=r.GetBoolean(13),seen=!r.IsDBNull(14),followUpAtUtc=r.IsDBNull(15)?(DateTimeOffset?)null:r.GetFieldValue<DateTimeOffset>(15),outcomes=Outcomes }); }
        return Result.Success<object>(new { items=rows,page=q.Page,pageSize,total });
    }

public async Task<Result<CartableWorkflowContextView>> WorkflowContextAsync(Guid id, CancellationToken ct) { DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "select node_key from barang.workflow_runtime_tasks where company_id=@company and id=@id", ("company", Company), ("id", id)); object? key = await q.ExecuteScalarAsync(ct); if (key is null) return Result.Failure<CartableWorkflowContextView>(Error.NotFound("workflow.task.not_found")); return Result.Success(new CartableWorkflowContextView(null, null, new("", key.ToString() ?? "", ""), null, true, [new("Approve", null, true), new("Reject", null, false)])); }
    public async Task<Result<CartableWorkloadSummary>> WorkloadSummaryAsync(CancellationToken ct)
    {
        DbConnection c=db.Database.GetDbConnection(); if(c.State!=ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c,ct);
        await using DbCommand q=Cmd(c,"select count(*) filter(where t.status in ('Open','Claimed')),count(*) filter(where t.status in ('Open','Claimed') and t.created_at_utc<=@attention),count(*) filter(where t.status in ('Open','Claimed') and t.created_at_utc<=@long_wait),count(*) filter(where ps.follow_up_at_utc is not null and ps.follow_up_at_utc<=@now) from barang.workflow_runtime_tasks t left join barang.cartable_personal_states ps on ps.company_id=t.company_id and ps.runtime_task_id=t.id and ps.user_id=@actor where t.company_id=@company and (t.claimed_on_behalf_of_user_id=@actor or exists(select 1 from barang.application_users au join barang.position_assignments pa on pa.company_id=t.company_id and pa.person_id=au.person_id and pa.position_id=t.position_id where au.id=@actor and pa.status='Active' and (pa.start_date is null or pa.start_date<=current_date) and (pa.end_date is null or pa.end_date>=current_date)) or exists(select 1 from barang.user_roles ur join barang.roles rr on rr.id=ur.role_id where ur.company_id=t.company_id and ur.user_id=@actor and rr.code='system.administrator' and rr.is_active))",("company",Company),("actor",Actor),("now",Now),("attention",Now.AddHours(-CartableAgingPolicy.AttentionHours)),("long_wait",Now.AddHours(-CartableAgingPolicy.LongWaitingHours)));
        await using DbDataReader r=await q.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) return Result.Success(new CartableWorkloadSummary(0,0,0,0)); return Result.Success(new CartableWorkloadSummary(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetInt32(3)));
    }
    public async Task<Result<IReadOnlyList<object>>> TasksAsync(Guid instanceId, CancellationToken ct)
    { DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); List<object> rows = []; await using DbCommand q = Cmd(c, "select id,node_key,status,created_at_utc,completed_at_utc,version from barang.workflow_runtime_tasks where company_id=@company and workflow_instance_id=@instance order by created_at_utc,id", ("company", Company), ("instance", instanceId)); await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new { id = r.GetGuid(0), nodeKey = r.GetString(1), status = r.GetString(2), createdAtUtc = r.GetFieldValue<DateTimeOffset>(3), completedAtUtc = r.IsDBNull(4) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(4), version = r.GetInt64(5) }); return Result.Success<IReadOnlyList<object>>(rows); }
    public Task<Result<object>> QuickPreviewAsync(Guid id, CancellationToken ct) => Task.FromResult(Result.Success<object>(new { id }));
    public async Task<Result<object>> SetPersonalStateAsync(Guid taskId, CartablePersonalStateCommand command, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct);
        await using DbCommand q = Cmd(c, "insert into barang.cartable_personal_states(id,company_id,user_id,runtime_task_id,is_starred,seen_at_utc,version,follow_up_at_utc) values(@id,@company,@user,@task,coalesce(@starred,false),case when @seen then @now else null end,1,@follow) on conflict (company_id,user_id,runtime_task_id) do update set is_starred=coalesce(@starred,cartable_personal_states.is_starred),seen_at_utc=case when @seen then @now else cartable_personal_states.seen_at_utc end,follow_up_at_utc=@follow,version=cartable_personal_states.version+1", ("id", Guid.NewGuid()), ("company", Company), ("user", Actor), ("task", taskId), ("starred", command.IsStarred), ("seen", command.Seen ?? false), ("now", Now), ("follow", command.FollowUpAtUtc));
        await q.ExecuteNonQueryAsync(ct); return Result.Success<object>(new { taskId, isStarred = command.IsStarred, seen = command.Seen, followUpAtUtc = command.FollowUpAtUtc });
    }
    public Task<Result<object>> WorkbenchAsync(CartableWorkbenchQuery query, CancellationToken ct) => TaskPage(query.Query, query.Historical, ct);
    public async Task<Result<CartableWorkbenchDetailView>> WorkbenchDetailAsync(Guid id, bool historical, CancellationToken ct)
    { DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "select o.id,o.order_number,o.order_date,o.status,o.priority,c.display_name,count(i.id),coalesce(sum(i.quantity),0) from barang.workflow_runtime_tasks t join barang.workflow_instances wi on wi.id=t.workflow_instance_id and wi.company_id=t.company_id join barang.orders o on o.id=wi.order_id and o.company_id=wi.company_id left join barang.customers c on c.id=o.customer_id and c.company_id=o.company_id left join barang.order_items i on i.order_id=o.id and i.company_id=o.company_id and i.is_active where t.company_id=@company and t.id=@id group by o.id,o.order_number,o.order_date,o.status,o.priority,c.display_name", ("company", Company), ("id", id)); await using DbDataReader r = await q.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) return Result.Failure<CartableWorkbenchDetailView>(Error.NotFound("workflow.task.not_found")); Guid orderId = r.GetGuid(0); CartableWorkbenchSummary summary = new(r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(5) ? null : r.GetString(5), r.GetFieldValue<DateOnly>(2), r.GetString(3), [], r.IsDBNull(4) ? "Normal" : r.GetString(4), r.GetInt32(6), r.GetDecimal(7), null, null, null); return Result.Success(new CartableWorkbenchDetailView(orderId, summary, null)); }
    public async Task<Result<IReadOnlyList<object>>> WorkbenchHistoryAsync(Guid id, CancellationToken ct)
    {
        DbConnection c=db.Database.GetDbConnection(); if(c.State!=ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c,ct); List<object> rows=[];
        await using DbCommand q=Cmd(c,"select h.id,h.event_type,h.occurred_at_utc,h.actor_user_id,h.summary,t.node_key from barang.workflow_history h left join barang.workflow_runtime_tasks t on t.company_id=h.company_id and t.id=h.human_task_id where h.company_id=@company and h.workflow_instance_id=(select workflow_instance_id from barang.workflow_runtime_tasks where company_id=@company and id=@task) order by h.occurred_at_utc,h.id",("company",Company),("task",id));
        await using DbDataReader r=await q.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) rows.Add(new{id=r.GetGuid(0),eventType=r.GetString(1),occurredAtUtc=r.GetFieldValue<DateTimeOffset>(2),actorUserId=r.IsDBNull(3)?(Guid?)null:r.GetGuid(3),summary=r.IsDBNull(4)?null:r.GetString(4),nodeKey=r.IsDBNull(5)?null:r.GetString(5)}); return Result.Success<IReadOnlyList<object>>(rows);
    }
    public async Task<Result<IReadOnlyList<object>>> DecisionAttachmentsAsync(Guid orderId, CancellationToken ct)
    { DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); List<object> rows = []; await using DbCommand q = Cmd(c, "select a.id,a.runtime_task_id,a.file_id,a.file_version_id,v.original_file_name,v.media_type,v.size_bytes,a.created_at_utc from barang.workflow_decision_attachments a join barang.file_versions v on v.company_id=a.company_id and v.id=a.file_version_id where a.company_id=@company and a.order_id=@order order by a.created_at_utc,a.id", ("company", Company), ("order", orderId)); await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new DecisionAttachmentView(r.GetGuid(0), r.GetGuid(1), r.GetGuid(2), r.GetGuid(3), r.GetString(4), r.GetString(5), r.GetInt64(6), r.GetFieldValue<DateTimeOffset>(7))); return Result.Success<IReadOnlyList<object>>(rows); }
    public async Task<Result<WorkflowFileDownload>> DownloadDecisionAttachmentAsync(Guid id, CancellationToken ct) { DbConnection c = db.Database.GetDbConnection(); await c.OpenAsync(ct); await SetCompany(c, ct); await using DbCommand q = Cmd(c, "select file_id,file_version_id from barang.workflow_decision_attachments where company_id=@company and id=@id", ("company", Company), ("id", id)); await using DbDataReader r = await q.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) return Result.Failure<WorkflowFileDownload>(Error.NotFound("workflow.decision_attachment.not_found")); Guid fileId = r.GetGuid(0), versionId = r.GetGuid(1); Result<DownloadFile> f = await files.DownloadAsync(fileId, versionId, ct); return f.IsFailure ? Result.Failure<WorkflowFileDownload>(f.Error) : Result.Success(new WorkflowFileDownload(f.Value.FileName, f.Value.MediaType, f.Value.Length, f.Value.Content)); }
    public async Task<Result<IReadOnlyList<object>>> ForwardTargetsAsync(Guid id, string? search, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct);
        List<object> rows = []; string text = search?.Trim() ?? "";
        await using DbCommand q = Cmd(c, "select distinct u.id,coalesce(p.display_name,u.display_name),coalesce(pos.title,'—') from barang.workflow_runtime_tasks t join barang.application_users u on u.id<>@actor and u.account_state='Active' join barang.user_companies uc on uc.user_id=u.id and uc.company_id=t.company_id and (uc.valid_to_utc is null or uc.valid_to_utc>@now) left join barang.position_assignments pa on pa.company_id=t.company_id and pa.person_id=u.person_id and pa.status='Active' and (pa.start_date is null or pa.start_date<=current_date) and (pa.end_date is null or pa.end_date>=current_date) left join barang.positions pos on pos.company_id=pa.company_id and pos.id=pa.position_id left join barang.persons p on p.company_id=pa.company_id and p.id=pa.person_id where t.company_id=@company and t.id=@task and (pa.position_id=t.position_id or exists(select 1 from barang.user_roles ur join barang.roles r on r.id=ur.role_id where ur.company_id=@company and ur.user_id=u.id and r.code='system.administrator' and r.is_active)) and (@search='' or coalesce(p.display_name,u.display_name) ilike '%'||@search||'%') order by 2,3", ("actor", Actor), ("now", Now), ("company", Company), ("task", id), ("search", text));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new { userId = r.GetGuid(0), personName = r.GetString(1), positionTitle = r.GetString(2) });
        return Result.Success<IReadOnlyList<object>>(rows);
    }
    public Task<Result<object>> ForwardTaskAsync(Guid id, ControlledForwardCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (command.TargetUserId == Actor || string.IsNullOrWhiteSpace(command.Reason)) return Result.Failure<object>(Error.Validation("workflow.forward.invalid"));
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(t); await SetCompany(c, t); DbTransaction? tr = db.Database.CurrentTransaction?.GetDbTransaction();
        await using DbCommand q = CmdTx(c, "update barang.workflow_runtime_tasks set claimed_on_behalf_of_user_id=@target,claimed_by_user_id=null,claimed_at_utc=null,version=version+1 where company_id=@company and id=@id and version=@version and status in ('Open','Claimed') and (claimed_by_user_id is null or claimed_by_user_id=@actor) and exists(select 1 from barang.application_users u join barang.user_companies uc on uc.user_id=u.id and uc.company_id=@company and (uc.valid_to_utc is null or uc.valid_to_utc>@now) where u.id=@target and u.account_state='Active') and (exists(select 1 from barang.position_assignments pa join barang.application_users u on u.person_id=pa.person_id and u.id=@target where pa.company_id=@company and pa.position_id=workflow_runtime_tasks.position_id and pa.status='Active' and (pa.start_date is null or pa.start_date<=current_date) and (pa.end_date is null or pa.end_date>=current_date)) or exists(select 1 from barang.user_roles ur join barang.roles r on r.id=ur.role_id where ur.company_id=@company and ur.user_id=@target and r.code='system.administrator' and r.is_active)) returning version", ("target", command.TargetUserId), ("company", Company), ("id", id), ("version", command.Version), ("actor", Actor), ("now", Now), tr);
        object? value = await q.ExecuteScalarAsync(t); if (value is null) return Result.Failure<object>(Error.BusinessRule("workflow.forward.not_eligible"));
        await using DbCommand instanceQuery = CmdTx(c, "select workflow_instance_id from barang.workflow_runtime_tasks where company_id=@company and id=@id", ("company", Company), ("id", id), tr); Guid instance = (Guid)(await instanceQuery.ExecuteScalarAsync(t) ?? Guid.Empty);
        await History(c, tr, instance, id, "TaskForwarded", command.Reason, t);
        return Result.Success<object>(new { id, forwardedToUserId = command.TargetUserId, version = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture) });
    }, ct);
    public async Task<Result<IReadOnlyList<object>>> ListSubstitutionsAsync(string? search, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct); List<object> rows = []; string text = search?.Trim() ?? "";
        await using DbCommand q = Cmd(c, "select s.id,s.principal_user_id,coalesce(pp.display_name,pu.display_name),s.substitute_user_id,coalesce(sp.display_name,su.display_name),s.effective_from_utc,s.effective_to_utc,s.is_active,s.reason,s.version,coalesce(cu.display_name,'—'),s.revoked_at_utc from barang.workflow_substitutions s join barang.application_users pu on pu.id=s.principal_user_id join barang.application_users su on su.id=s.substitute_user_id left join barang.persons pp on pp.id=pu.person_id left join barang.persons sp on sp.id=su.person_id left join barang.application_users cu on cu.id=s.created_by_user_id where s.company_id=@company and (@search='' or coalesce(pp.display_name,pu.display_name) ilike '%'||@search||'%' or coalesce(sp.display_name,su.display_name) ilike '%'||@search||'%' or coalesce(s.reason,'') ilike '%'||@search||'%') order by s.effective_from_utc desc,s.id", ("company", Company), ("search", text));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new { id=r.GetGuid(0), principalUserId=r.GetGuid(1), principalName=r.GetString(2), substituteUserId=r.GetGuid(3), substituteName=r.GetString(4), effectiveFromUtc=r.GetFieldValue<DateTimeOffset>(5), effectiveToUtc=r.GetFieldValue<DateTimeOffset>(6), isActive=r.GetBoolean(7) && r.IsDBNull(11), reason=r.IsDBNull(8)?null:r.GetString(8), version=r.GetInt64(9), createdByName=r.GetString(10), revokedAtUtc=r.IsDBNull(11)?(DateTimeOffset?)null:r.GetFieldValue<DateTimeOffset>(11) });
        return Result.Success<IReadOnlyList<object>>(rows);
    }
    public async Task<Result<IReadOnlyList<object>>> SubstitutionCandidatesAsync(string? search, CancellationToken ct)
    {
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(ct); await SetCompany(c, ct); List<object> rows = []; string text = search?.Trim() ?? "";
        await using DbCommand q = Cmd(c, "select distinct u.id,coalesce(p.display_name,u.display_name),u.username,coalesce(pos.title,'—') from barang.application_users u join barang.user_companies uc on uc.user_id=u.id and uc.company_id=@company and (uc.valid_to_utc is null or uc.valid_to_utc>@now) left join barang.persons p on p.id=u.person_id left join barang.position_assignments pa on pa.company_id=@company and pa.person_id=u.person_id and pa.status='Active' left join barang.positions pos on pos.company_id=pa.company_id and pos.id=pa.position_id where u.account_state='Active' and (@search='' or coalesce(p.display_name,u.display_name) ilike '%'||@search||'%' or u.username ilike '%'||@search||'%') order by 2", ("company", Company), ("now", Now), ("search", text));
        await using DbDataReader r = await q.ExecuteReaderAsync(ct); while (await r.ReadAsync(ct)) rows.Add(new { userId=r.GetGuid(0), personName=r.GetString(1), username=r.GetString(2), positionTitle=r.GetString(3) }); return Result.Success<IReadOnlyList<object>>(rows);
    }
    public Task<Result<object>> CreateSubstitutionAsync(CreateWorkflowSubstitutionCommand command, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        if (command.PrincipalUserId == command.SubstituteUserId || command.EffectiveToUtc <= command.EffectiveFromUtc) return Result.Failure<object>(Error.Validation("workflow.substitution.invalid"));
        DbConnection c = db.Database.GetDbConnection(); if (c.State != ConnectionState.Open) await c.OpenAsync(t); await SetCompany(c, t); DbTransaction? tr=db.Database.CurrentTransaction?.GetDbTransaction();
        await using DbCommand q=CmdTx(c,"insert into barang.workflow_substitutions(id,company_id,version,principal_user_id,substitute_user_id,effective_from_utc,effective_to_utc,is_active,reason,created_by_user_id,created_at_utc) select @id,@company,1,@principal,@substitute,@from,@to,true,@reason,@actor,@now where exists(select 1 from barang.application_users u join barang.user_companies uc on uc.user_id=u.id and uc.company_id=@company and (uc.valid_to_utc is null or uc.valid_to_utc>@now) where u.id=@principal and u.account_state='Active') and exists(select 1 from barang.application_users u join barang.user_companies uc on uc.user_id=u.id and uc.company_id=@company and (uc.valid_to_utc is null or uc.valid_to_utc>@now) where u.id=@substitute and u.account_state='Active') and not exists(select 1 from barang.workflow_substitutions s where s.company_id=@company and s.principal_user_id=@principal and s.is_active and s.revoked_at_utc is null and s.effective_from_utc<@to and s.effective_to_utc>@from) returning id,version", ("id",Guid.NewGuid()),("company",Company),("principal",command.PrincipalUserId),("substitute",command.SubstituteUserId),("from",command.EffectiveFromUtc),("to",command.EffectiveToUtc),("reason",command.Reason),("actor",Actor),("now",Now),tr);
        await using DbDataReader r=await q.ExecuteReaderAsync(t); if(!await r.ReadAsync(t)) return Result.Failure<object>(Error.BusinessRule("workflow.substitution.not_eligible")); return Result.Success<object>(new{id=r.GetGuid(0),version=r.GetInt64(1)});
    },ct);
    public Task<Result<object>> RevokeSubstitutionAsync(Guid id, long version, CancellationToken ct) => tx.ExecuteAsync(async t =>
    {
        DbConnection c=db.Database.GetDbConnection(); if(c.State!=ConnectionState.Open) await c.OpenAsync(t); await SetCompany(c,t); DbTransaction? tr=db.Database.CurrentTransaction?.GetDbTransaction();
        await using DbCommand q=CmdTx(c,"update barang.workflow_substitutions set is_active=false,revoked_at_utc=@now,version=version+1 where company_id=@company and id=@id and version=@version and is_active and revoked_at_utc is null returning version",("company",Company),("id",id),("version",version),("now",Now),tr); object? value=await q.ExecuteScalarAsync(t); return value is null?Result.Failure<object>(Error.Concurrency("workflow.substitution.concurrency")):Result.Success<object>(new{id,version=Convert.ToInt64(value,System.Globalization.CultureInfo.InvariantCulture)});
    },ct);

    private Task CreateNextTasks(DbConnection c, DbTransaction? tr, Guid instance, Guid version, Guid fromNode, CancellationToken ct, string? outcome = null) => Advance(c, tr, instance, fromNode, outcome, 0, ct);
    private async Task Advance(DbConnection c, DbTransaction? tr, Guid instance, Guid fromNode, string? outcome, int depth, CancellationToken ct)
    {
        if (depth > 32) throw new InvalidOperationException("workflow.definition.cycle");
        string sourceType;
        await using (DbCommand source = CmdTx(c, "select type from barang.workflow_definition_nodes where company_id=@company and id=@id", ("company", Company), ("id", fromNode), tr)) sourceType = Convert.ToString(await source.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) ?? "";
        List<(Guid Id, string Key, string Type, Guid? Position, string Condition, bool Default)> next = [];
        await using (DbCommand q = CmdTx(c, "select to_node.id,to_node.node_key,to_node.type,to_node.position_id,tr.condition_json,tr.is_default from barang.workflow_definition_transitions tr join barang.workflow_definition_nodes to_node on to_node.id=tr.to_node_id where tr.company_id=@company and tr.from_node_id=@from order by tr.sort_order,tr.id", ("company", Company), ("from", fromNode), tr))
        await using (DbDataReader r = await q.ExecuteReaderAsync(ct))
        { while (await r.ReadAsync(ct)) next.Add((r.GetGuid(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetGuid(3), r.GetString(4), r.GetBoolean(5))); }
        foreach (var x in next)
        {
            if (sourceType.EndsWith("Gateway", StringComparison.OrdinalIgnoreCase) && outcome is not null)
            {
                using JsonDocument condition = JsonDocument.Parse(x.Condition); string? edgeOutcome = condition.RootElement.TryGetProperty("outcome", out var o) && o.ValueKind == JsonValueKind.String ? o.GetString() : null;
                if (!string.Equals(edgeOutcome, outcome, StringComparison.OrdinalIgnoreCase) && !(x.Default && string.IsNullOrWhiteSpace(edgeOutcome))) continue;
            }
            if (x.Type == "End")
            {
                await using DbCommand end = CmdTx(c, "update barang.workflow_instances set status='Completed',current_stage='Completed',completed_at_utc=@now,updated_at_utc=@now,version=version+1 where company_id=@company and id=@id", ("now", Now), ("company", Company), ("id", instance), tr); await end.ExecuteNonQueryAsync(ct); continue;
            }
            if (x.Type == "UserTask")
            {
                await using DbCommand ins = CmdTx(c, "insert into barang.workflow_runtime_tasks(id,company_id,workflow_instance_id,definition_node_id,node_key,position_id,status,created_at_utc,version,origin_node_id,origin_node_key) values(@id,@company,@instance,@node,@key,@position,'Open',@now,1,@origin,@originKey)", ("id", Guid.NewGuid()), ("company", Company), ("instance", instance), ("node", x.Id), ("key", x.Key), ("position", x.Position), ("now", Now), ("origin", fromNode), ("originKey", x.Key), tr); await ins.ExecuteNonQueryAsync(ct); await History(c, tr, instance, null, "TaskCreated", x.Key, ct); continue;
            }
            await Advance(c, tr, instance, x.Id, outcome, depth + 1, ct);
        }
    }
    private async Task<Guid> VersionForInstance(DbConnection c, DbTransaction? tr, Guid instance, CancellationToken ct) { await using DbCommand q = CmdTx(c, "select \"DefinitionVersionId\" from barang.workflow_instances where company_id=@company and id=@id", ("company", Company), ("id", instance), tr); return (Guid)(await q.ExecuteScalarAsync(ct) ?? Guid.Empty); }
    private async Task<Guid> History(DbConnection c, DbTransaction? tr, Guid instance, Guid? task, string eventType, string? summary, CancellationToken ct) { Guid id = Guid.NewGuid(); string? boundedSummary = summary is null ? null : summary.Length <= 500 ? summary : summary[..497] + "..."; await using DbCommand q = CmdTx(c, "insert into barang.workflow_history(id,company_id,workflow_instance_id,human_task_id,event_type,occurred_at_utc,actor_user_id,summary) values(@id,@company,@instance,@task,@event,@now,@actor,@summary)", ("id", id), ("company", Company), ("instance", instance), ("task", task), ("event", eventType), ("now", Now), ("actor", Actor), ("summary", boundedSummary), tr); await q.ExecuteNonQueryAsync(ct); return id; }
    private async Task SaveAttachments(DbConnection c, DbTransaction? tr, Guid historyId, Guid instance, Guid task, string? resultJson, IReadOnlyList<DecisionAttachmentCommand>? attachments, CancellationToken ct)
    { if (attachments is null || attachments.Count == 0) return; if (attachments.Count > DecisionAttachmentPolicy.MaximumCount) throw new InvalidOperationException("workflow.decision_attachment.too_many"); Guid order; string node; await using (DbCommand q = CmdTx(c, "select i.order_id,t.node_key from barang.workflow_instances i join barang.workflow_runtime_tasks t on t.workflow_instance_id=i.id and t.company_id=i.company_id where i.company_id=@company and i.id=@instance and t.id=@task", ("company", Company), ("instance", instance), ("task", task), tr)) await using (DbDataReader r = await q.ExecuteReaderAsync(ct)) { if (!await r.ReadAsync(ct)) throw new InvalidOperationException("workflow.decision_attachment.invalid_task"); order = r.GetGuid(0); node = r.GetString(1); }
        foreach (DecisionAttachmentCommand a in attachments) { await using DbCommand q = CmdTx(c, "insert into barang.workflow_decision_attachments(id,company_id,order_id,workflow_instance_id,runtime_task_id,workflow_history_id,actor_user_id,node_key,outcome,file_id,file_version_id,created_at_utc) select @id,@company,@order,@instance,@task,@history,@actor,@node,'Decision',@file,@version,@now where exists(select 1 from barang.file_versions v where v.company_id=@company and v.id=@version and v.file_id=@file and not v.is_archived)", ("id", Guid.NewGuid()), ("company", Company), ("order", order), ("instance", instance), ("task", task), ("history", historyId), ("actor", Actor), ("node", node), ("file", a.FileId), ("version", a.FileVersionId), ("now", Now), tr); if (await q.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("workflow.decision_attachment.invalid_file"); }
    }
    private async Task SetCompany(DbConnection c, CancellationToken ct) { await using DbCommand q = c.CreateCommand(); q.CommandText = "select set_config('barang.company_id',cast(@company as text),false)"; DbParameter p = q.CreateParameter(); p.ParameterName = "company"; p.Value = Company; q.Parameters.Add(p); await q.ExecuteScalarAsync(ct); }
    private async Task<bool> WarehouseIssueEligible(DbConnection c, DbTransaction? tr, Guid orderId, CancellationToken ct)
    {
        await using DbCommand q = CmdTx(c, "select exists(select 1 from barang.stock_reservations r where r.company_id=@company and r.order_item_id in (select id from barang.order_items where company_id=@company and order_id=@order) and r.status in ('Active','PartiallyReleased') and exists(select 1 from barang.warehouse_responsibilities wr join barang.application_users au on au.id=wr.application_user_id join barang.user_companies uc on uc.user_id=au.id and uc.company_id=wr.company_id where wr.company_id=@company and wr.warehouse_id=r.warehouse_id and wr.application_user_id=@actor and wr.is_active and au.account_state='Active' and uc.valid_from_utc<=@now and (uc.valid_to_utc is null or uc.valid_to_utc>@now)))", ("company", Company), ("order", orderId), ("actor", Actor), ("now", Now), tr);
        return Convert.ToBoolean(await q.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }
    private static DbCommand Cmd(DbConnection c, string sql, params (string Name, object? Value)[] values) { DbCommand q = c.CreateCommand(); q.CommandText = sql; foreach (var (name, value) in values) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; q.Parameters.Add(p); } return q; }
    private static DbCommand CmdTx(DbConnection c, string sql, params object?[] values) { DbTransaction? transaction = values.OfType<DbTransaction>().FirstOrDefault(); DbCommand q = c.CreateCommand(); q.CommandText = sql; q.Transaction = transaction; foreach (object? value in values) { if (value is DbTransaction) continue; if (value is System.Runtime.CompilerServices.ITuple item && item.Length == 2 && item[0] is string name) { DbParameter p = q.CreateParameter(); p.ParameterName = name; p.Value = item[1] ?? DBNull.Value; q.Parameters.Add(p); } } return q; }
}








