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
public Task<Result<NotificationPage>>MyAsync(bool?unread,int page,int size,CancellationToken ct)=>tx.ExecuteAsync(async t=>{page=Math.Max(1,page);size=Math.Clamp(size,1,100);IQueryable<Notification>q=db.Notifications.AsNoTracking().Where(x=>x.RecipientUserId==Actor);if(unread==true)q=q.Where(x=>x.ReadAtUtc==null);if(unread==false)q=q.Where(x=>x.ReadAtUtc!=null);long total=await q.LongCountAsync(t);var rows=await q.OrderByDescending(x=>x.CreatedAtUtc).ThenByDescending(x=>x.Id).Skip((page-1)*size).Take(size).ToListAsync(t);return Result.Success(new NotificationPage(rows.Select(View).ToArray(),page,size,total));},ct);
public Task<Result>MarkReadAsync(Guid id,long version,CancellationToken ct)=>tx.ExecuteAsync(async t=>{Notification?n=await db.Notifications.SingleOrDefaultAsync(x=>x.Id==id&&x.RecipientUserId==Actor,t);if(n is null)return Result.Failure(BackgroundErrors.NotFound);Result r=n.Read(Actor,version,Now);if(r.IsSuccess)audit.Write(new("notification.read","notification",id.ToString(),ChangeKind:AuditChangeKind.Modified));return r;},ct);
public Task<Result<int>>MarkAllReadAsync(CancellationToken ct)=>tx.ExecuteAsync(async t=>{List<Notification>rows=await db.Notifications.Where(x=>x.RecipientUserId==Actor&&x.ReadAtUtc==null).ToListAsync(t);int count=0;foreach(var n in rows)if(n.Read(Actor,n.Version,Now).IsSuccess)count++;if(count>0)audit.Write(new("notification.read_all","notification",Actor.ToString(),ChangeKind:AuditChangeKind.Modified));return Result.Success(count);},ct);
public Task<Result<IReadOnlyList<DeadLetterView>>>DeadLettersAsync(CancellationToken ct)=>tx.ExecuteAsync(async t=>Result.Success<IReadOnlyList<DeadLetterView>>((await db.DeadLetterItems.AsNoTracking().OrderByDescending(x=>x.CreatedAtUtc).Take(100).ToListAsync(t)).Select(View).ToArray()),ct);
public Task<Result>RetryAsync(Guid id,long version,CancellationToken ct)=>tx.ExecuteAsync(async t=>{DeadLetterItem?d=await db.DeadLetterItems.SingleOrDefaultAsync(x=>x.Id==id,t);if(d is null)return Result.Failure(BackgroundErrors.NotFound);Result r=d.RequestRetry(Actor,version,Now);if(r.IsFailure)return r;OutboxMessage?m=await db.OutboxMessages.SingleOrDefaultAsync(x=>x.Id==d.OutboxMessageId,t);if(m is null)return Result.Failure(BackgroundErrors.NotFound);m.ManualRetry(Now);audit.Write(new("backgroundjob.retry_requested","dead_letter",id.ToString(),ChangeKind:AuditChangeKind.Modified));return Result.Success();},ct);
private static NotificationView View(Notification x)=>new(x.Id,x.Channel,x.TemplateCode,x.Payload,x.Status,x.CreatedAtUtc,x.ReadAtUtc,x.Version);private static DeadLetterView View(DeadLetterItem x)=>new(x.Id,x.OutboxMessageId,x.ErrorCode,x.Status,x.CreatedAtUtc,x.Version);internal sealed record DispatchPayload(Guid RecipientUserId,NotificationChannel Channel,string TemplateCode,string Payload);
}
#pragma warning restore CA1725
