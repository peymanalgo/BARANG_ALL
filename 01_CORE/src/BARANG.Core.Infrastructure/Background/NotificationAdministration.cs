using System.Text.Json;using BARANG.Core.Application.Abstractions.Actors;using BARANG.Core.Application.Abstractions.Companies;using BARANG.Core.Application.Abstractions.Correlation;using BARANG.Core.Application.Abstractions.Transactions;using BARANG.Core.Application.Auditing;using BARANG.Core.Application.Background;using BARANG.Core.Domain.Background;using BARANG.Core.Domain.Common;using BARANG.Core.Infrastructure.Persistence;using Microsoft.EntityFrameworkCore;
namespace BARANG.Core.Infrastructure.Background;
#pragma warning disable CA1725
public sealed class NotificationAdministration(CoreDbContext db,ITransactionExecutor tx,ICompanyContext company,ICurrentActor current,ICorrelationContext correlation,IAuditWriter audit,TimeProvider time):INotificationAdministration
{private Guid Company=>company.RequireCompanyId();private Guid Actor=>Guid.TryParse(current.Actor.UserId,out Guid x)?x:throw new InvalidOperationException();private DateTimeOffset Now=>time.GetUtcNow();
public async Task PublishBusinessEventInsideTransactionAsync(string eventType, string stableEventKey, string templateCode, object payload, string recipientPermission, CancellationToken ct)
{
    Guid companyId = Company;
    if (db.Database.IsNpgsql())
    {
        string lockKey = companyId.ToString("N") + ":" + eventType + ":" + stableEventKey;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", ct);
    }

    IQueryable<Guid> direct = from uc in db.UserCompanies.IgnoreQueryFilters()
                              join u in db.ApplicationUsers.IgnoreQueryFilters() on uc.UserId equals u.Id
                              where uc.CompanyId == companyId
                                  && uc.ValidFromUtc <= Now
                                  && (uc.ValidToUtc == null || uc.ValidToUtc > Now)
                                  && u.AccountState == BARANG.Core.Domain.Security.UserAccountState.Active
                                  && db.UserPermissions.IgnoreQueryFilters().Any(up => up.UserId == uc.UserId && up.CompanyId == companyId
                                      && db.Permissions.IgnoreQueryFilters().Any(p => p.Id == up.PermissionId && p.Code == recipientPermission))
                              select uc.UserId;

    IQueryable<Guid> inherited = from uc in db.UserCompanies.IgnoreQueryFilters()
                                  join u in db.ApplicationUsers.IgnoreQueryFilters() on uc.UserId equals u.Id
                                  join ur in db.UserRoles.IgnoreQueryFilters() on new { uc.UserId, uc.CompanyId } equals new { ur.UserId, ur.CompanyId }
                                  join rp in db.RolePermissions.IgnoreQueryFilters() on ur.RoleId equals rp.RoleId
                                  join p in db.Permissions.IgnoreQueryFilters() on rp.PermissionId equals p.Id
                                  join r in db.Roles.IgnoreQueryFilters() on ur.RoleId equals r.Id
                                  where uc.CompanyId == companyId
                                      && uc.ValidFromUtc <= Now
                                      && (uc.ValidToUtc == null || uc.ValidToUtc > Now)
                                      && ur.ValidFromUtc <= Now
                                      && (ur.ValidToUtc == null || ur.ValidToUtc > Now)
                                      && u.AccountState == BARANG.Core.Domain.Security.UserAccountState.Active
                                      && r.IsActive
                                      && p.Code == recipientPermission
                                  select uc.UserId;

    Guid[] recipients = await direct.Concat(inherited).Distinct().ToArrayAsync(ct);
    string body = JsonSerializer.Serialize(new { eventType, templateCode, payload });
    foreach (Guid recipient in recipients)
    {
        string key = $"{eventType}:{stableEventKey}:{recipient:N}";
        if (await db.OutboxMessages.IgnoreQueryFilters().AnyAsync(x => x.CompanyId == companyId && x.EventType == "notification.requested" && x.CorrelationId == key, ct))
        {
            continue;
        }

        Result<OutboxMessage> made = OutboxMessage.Create(
            companyId,
            "notification.requested",
            JsonSerializer.Serialize(new { recipientUserId = recipient, channel = NotificationChannel.InApp, templateCode, payload = body }),
            key,
            Now);
        if (made.IsSuccess) db.OutboxMessages.Add(made.Value);
    }
}
public Task<Result<Guid>>QueueAsync(NotificationRequest r,CancellationToken ct)=>tx.ExecuteAsync(async t=>{if(!await db.UserCompanies.IgnoreQueryFilters().AnyAsync(x=>x.CompanyId==Company&&x.UserId==r.RecipientUserId&&x.ValidToUtc==null,t))return Result.Failure<Guid>(BackgroundErrors.Invalid);string payload=JsonSerializer.Serialize(new DispatchPayload(r.RecipientUserId,r.Channel,r.TemplateCode,r.Payload));Result<OutboxMessage>made=OutboxMessage.Create(Company,"notification.requested",payload,correlation.CorrelationId??Guid.NewGuid().ToString("N"),Now);if(made.IsFailure)return Result.Failure<Guid>(made.Error);db.Add(made.Value);audit.Write(new("notification.queued","outbox",made.Value.Id.ToString(),ChangeKind:AuditChangeKind.Added));return Result.Success(made.Value.Id);},ct);
public Task<Result<NotificationPage>>MyAsync(bool?unread,int page,int size,CancellationToken ct)=>MyAsync(new(unread,null,null,null,null,null,null,page,size),ct);
public Task<Result<NotificationPage>>MyAsync(NotificationQuery query,CancellationToken ct)=>tx.ExecuteAsync(async t=>{int page=Math.Max(1,query.Page);int size=Math.Clamp(query.PageSize,1,100);IQueryable<Notification>q=db.Notifications.AsNoTracking().Where(x=>x.RecipientUserId==Actor);if(query.Actionable==true)q=q.Where(x=>x.TemplateCode.Contains("assigned")||x.TemplateCode.Contains("shortage")||x.TemplateCode.Contains("approval")||x.TemplateCode.Contains("recheck")||x.TemplateCode.Contains("reproduction")||x.TemplateCode.Contains("shipment")||x.TemplateCode.Contains("delivery")||x.TemplateCode.Contains("invoice"));if(query.Unread==true)q=q.Where(x=>x.ReadAtUtc==null);if(query.Unread==false)q=q.Where(x=>x.ReadAtUtc!=null);if(query.FromUtc is not null)q=q.Where(x=>x.CreatedAtUtc>=query.FromUtc);if(query.ToUtc is not null)q=q.Where(x=>x.CreatedAtUtc<query.ToUtc);if(!string.IsNullOrWhiteSpace(query.Category))q=CategoryFilter(q,query.Category);if(!string.IsNullOrWhiteSpace(query.Priority))q=PriorityFilter(q,query.Priority);IQueryable<Notification>ordered=q.OrderByDescending(x=>x.CreatedAtUtc).ThenByDescending(x=>x.Id);List<Notification>candidate;string? search=query.Search?.Trim();if(!string.IsNullOrWhiteSpace(search)){candidate=await ordered.Take(5000).ToListAsync(t);candidate=candidate.Where(x=>x.TemplateCode.Contains(search,StringComparison.OrdinalIgnoreCase)||x.Payload.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();}else candidate=await ordered.ToListAsync(t);long total=candidate.Count;long unreadTotal=candidate.LongCount(x=>x.ReadAtUtc==null);List<Notification> rows=candidate.Skip((page-1)*size).Take(size).ToList();return Result.Success(new NotificationPage(rows.Select(View).ToArray(),page,size,total,unreadTotal));},ct);
public Task<Result>MarkReadAsync(Guid id,long version,CancellationToken ct)=>tx.ExecuteAsync(async t=>{Notification?n=await db.Notifications.SingleOrDefaultAsync(x=>x.Id==id&&x.RecipientUserId==Actor,t);if(n is null)return Result.Failure(BackgroundErrors.NotFound);Result r=n.Read(Actor,version,Now);if(r.IsSuccess)audit.Write(new("notification.read","notification",id.ToString(),ChangeKind:AuditChangeKind.Modified));return r;},ct);
public Task<Result<int>>MarkAllReadAsync(CancellationToken ct)=>tx.ExecuteAsync(async t=>{List<Notification>rows=await db.Notifications.Where(x=>x.RecipientUserId==Actor&&x.ReadAtUtc==null).ToListAsync(t);int count=0;foreach(var n in rows)if(n.Read(Actor,n.Version,Now).IsSuccess)count++;if(count>0)audit.Write(new("notification.read_all","notification",Actor.ToString(),ChangeKind:AuditChangeKind.Modified));return Result.Success(count);},ct);
public Task<Result<IReadOnlyList<DeadLetterView>>>DeadLettersAsync(CancellationToken ct)=>tx.ExecuteAsync(async t=>Result.Success<IReadOnlyList<DeadLetterView>>((await db.DeadLetterItems.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(t)).Select(View).ToArray()),ct);
public Task<Result>RetryAsync(Guid id,long version,CancellationToken ct)=>tx.ExecuteAsync(async t=>{DeadLetterItem?d=await db.DeadLetterItems.SingleOrDefaultAsync(x=>x.Id==id,t);if(d is null)return Result.Failure(BackgroundErrors.NotFound);Result r=d.RequestRetry(Actor,version,Now);if(r.IsFailure)return r;OutboxMessage?m=await db.OutboxMessages.SingleOrDefaultAsync(x=>x.Id==d.OutboxMessageId,t);if(m is null)return Result.Failure(BackgroundErrors.NotFound);m.ManualRetry(Now);audit.Write(new("backgroundjob.retry_requested","dead_letter",id.ToString(),ChangeKind:AuditChangeKind.Modified));return Result.Success();},ct);
private static IQueryable<Notification> CategoryFilter(IQueryable<Notification> q,string category)=>category.Trim().ToLowerInvariant() switch {"workflow"=>q.Where(x=>x.TemplateCode.Contains("workflow")),"inventory"=>q.Where(x=>x.TemplateCode.Contains("inventory")||x.TemplateCode.Contains("shortage")||x.TemplateCode.Contains("receipt")),"purchasing"=>q.Where(x=>x.TemplateCode.Contains("purchase")||x.TemplateCode.Contains("procurement")),"finance"=>q.Where(x=>x.TemplateCode.Contains("finance")||x.TemplateCode.Contains("invoice")||x.TemplateCode.Contains("payment")),"quality"=>q.Where(x=>x.TemplateCode.Contains("quality")||x.TemplateCode.Contains("reproduction")),"logistics"=>q.Where(x=>x.TemplateCode.Contains("shipment")||x.TemplateCode.Contains("delivery")||x.TemplateCode.Contains("return")),"order"=>q.Where(x=>x.TemplateCode.Contains("order")),"sla"=>q.Where(x=>x.TemplateCode.Contains("sla")||x.TemplateCode.Contains("escalat")),_=>q.Where(x=>false)};
private static IQueryable<Notification> PriorityFilter(IQueryable<Notification> q,string priority)=>priority.Trim().ToLowerInvariant() switch {"urgent"=>q.Where(x=>x.TemplateCode.Contains("shortage")||x.TemplateCode.Contains("rejected")||x.TemplateCode.Contains("escalat")||x.TemplateCode.Contains("failure")),"attention"=>q.Where(x=>x.TemplateCode.Contains("approval")||x.TemplateCode.Contains("assigned")||x.TemplateCode.Contains("recheck")||x.TemplateCode.Contains("reproduction")),"info"=>q.Where(x=>!x.TemplateCode.Contains("shortage")&&!x.TemplateCode.Contains("rejected")&&!x.TemplateCode.Contains("escalat")&&!x.TemplateCode.Contains("failure")&&!x.TemplateCode.Contains("approval")&&!x.TemplateCode.Contains("assigned")&&!x.TemplateCode.Contains("recheck")&&!x.TemplateCode.Contains("reproduction")),_=>q.Where(x=>false)};
private static NotificationView View(Notification x){using JsonDocument doc=JsonDocument.Parse(x.Payload);JsonElement root=doc.RootElement;string eventType=root.TryGetProperty("eventType",out JsonElement et)?et.GetString()??x.TemplateCode:x.TemplateCode;JsonElement payload=root.TryGetProperty("payload",out JsonElement p)?p:root;string category=Category(eventType);string priority=Priority(eventType);bool actionable=Actionable(eventType);string? reference=First(payload,"orderNumber","orderCode","reference","shipmentNumber","purchaseRequestNumber","invoiceNumber","taskId","sourceReference");string? route=Route(eventType,payload);string summary=Summary(eventType,payload);string? action=actionable?ActionCode(eventType):null;string state=payload.TryGetProperty("status",out JsonElement status)?status.GetString()??"pending":"pending";return new(x.Id,x.Channel,x.TemplateCode,x.Payload,x.Status,x.CreatedAtUtc,x.ReadAtUtc,x.Version,category,priority,actionable,reference,route,summary,action,state);}
private static string Category(string value)=>value.ToLowerInvariant() switch {var x when x.Contains("workflow")=>"workflow",var x when x.Contains("inventory")||x.Contains("shortage")||x.Contains("receipt")=>"inventory",var x when x.Contains("purchase")||x.Contains("procurement")=>"purchasing",var x when x.Contains("finance")||x.Contains("invoice")||x.Contains("payment")=>"finance",var x when x.Contains("quality")||x.Contains("reproduction")=>"quality",var x when x.Contains("shipment")||x.Contains("delivery")||x.Contains("return")=>"logistics",var x when x.Contains("order")=>"order",var x when x.Contains("sla")||x.Contains("escalat")=>"sla",_=>"system"};
private static string Priority(string value){string x=value.ToLowerInvariant();return x.Contains("shortage")||x.Contains("rejected")||x.Contains("escalat")||x.Contains("failure")?"urgent":x.Contains("approval")||x.Contains("assigned")||x.Contains("recheck")||x.Contains("reproduction")?"attention":"info";}
private static bool Actionable(string value){string x=value.ToLowerInvariant();return x.Contains("assigned")||x.Contains("shortage")||x.Contains("approval")||x.Contains("recheck")||x.Contains("reproduction")||x.Contains("shipment")||x.Contains("delivery")||x.Contains("invoice");}
private static string? ActionCode(string value)=>Category(value) switch {"workflow"=>"open-task","inventory"=>"review-shortage","purchasing"=>"review-purchasing","finance"=>"review-finance","quality"=>"review-quality","logistics"=>"view-shipment",_=>"open"};
private static string Summary(string value,JsonElement payload)=>value switch {var x when x.Contains("shortage",StringComparison.OrdinalIgnoreCase)=>"inventory.shortage",var x when x.Contains("approval",StringComparison.OrdinalIgnoreCase)=>"approval.required",var x when x.Contains("shipment",StringComparison.OrdinalIgnoreCase)=>"shipment.update",var x when x.Contains("delivery",StringComparison.OrdinalIgnoreCase)=>"delivery.update",var x when x.Contains("assigned",StringComparison.OrdinalIgnoreCase)=>"task.assigned",_=>value};
private static string? Route(string value,JsonElement payload){if(payload.TryGetProperty("taskId",out _))return "/workspace/cartable";if(payload.TryGetProperty("orderId",out JsonElement order)&&order.ValueKind==JsonValueKind.String)return "/orders/"+order.GetString()+"/360";if(payload.TryGetProperty("shipmentId",out JsonElement shipment)&&shipment.ValueKind==JsonValueKind.String)return "/workspace/logistics/shipments/"+shipment.GetString();return null;}
private static string? First(JsonElement p,params string[] names){foreach(string name in names)if(p.TryGetProperty(name,out JsonElement value)&&value.ValueKind is JsonValueKind.String or JsonValueKind.Number)return value.ToString();return null;}
private static DeadLetterView View(DeadLetterItem x)=>new(x.Id,x.OutboxMessageId,x.ErrorCode,x.Status,x.CreatedAtUtc,x.Version);internal sealed record DispatchPayload(Guid RecipientUserId,NotificationChannel Channel,string TemplateCode,string Payload);
}
#pragma warning restore CA1725
