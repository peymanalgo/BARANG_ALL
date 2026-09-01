using System.Text.Json;
using BARANG.Core.Application.Abstractions.Transactions;
using BARANG.Core.Application.Background;
using BARANG.Core.Domain.Background;
using BARANG.Core.Domain.Common;
using BARANG.Core.Infrastructure.Context;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Background;

public sealed class OutboxDispatcher(CoreDbContext db, ITransactionExecutor transactions, CompanyContext companies, ISmsSender sms, IEmailSender email, IPushSender push, TimeProvider time) : IOutboxDispatcher
{
    public async Task<int> DispatchAllAsync(CancellationToken ct)
    {
        int processed = 0;
        Guid[] companyIds = await db.Companies.AsNoTracking().Where(x => x.IsActive).Select(x => x.Id).ToArrayAsync(ct);
        foreach (Guid companyId in companyIds)
        {
            using IDisposable scope = companies.BeginScope(companyId);
            Result<IReadOnlyList<Guid>> pending = await transactions.ExecuteAsync(async transactionToken => Result.Success<IReadOnlyList<Guid>>(await db.OutboxMessages.Where(x => x.CompanyId == companyId && x.Status == OutboxStatus.Pending && x.NextAttemptAtUtc <= time.GetUtcNow()).OrderBy(x => x.OccurredAtUtc).Take(100).Select(x => x.Id).ToListAsync(transactionToken)), ct);
            if (pending.IsFailure) continue;
            foreach (Guid id in pending.Value) if (await DispatchAsync(id, companyId, ct)) processed++;
        }
        return processed;
    }

    private async Task<bool> DispatchAsync(Guid id, Guid companyId, CancellationToken cancellationToken)
    {
        Result<bool> result = await transactions.ExecuteAsync(async transactionToken =>
        {
            if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM barang.outbox_messages WHERE company_id={companyId} AND id={id} FOR UPDATE SKIP LOCKED", transactionToken);
            OutboxMessage? message = await db.OutboxMessages.SingleOrDefaultAsync(x => x.Id == id, transactionToken);
            if (message is null || message.Status != OutboxStatus.Pending || message.NextAttemptAtUtc > time.GetUtcNow()) return Result.Success(false);
            message.Start(time.GetUtcNow());
            try
            {
                if (message.EventType != "notification.requested") { await MoveToDeadLetterAsync(message, "outbox.unsupported_event", transactionToken); return Result.Success(false); }
                NotificationAdministration.DispatchPayload? payload = JsonSerializer.Deserialize<NotificationAdministration.DispatchPayload>(message.Payload);
                if (payload is null) { await MoveToDeadLetterAsync(message, "outbox.invalid_payload", transactionToken); return Result.Success(false); }
                Result<Notification> creation = Notification.Create(companyId, payload.RecipientUserId, payload.Channel, payload.TemplateCode, payload.Payload, time.GetUtcNow());
                if (creation.IsFailure) { await MoveToDeadLetterAsync(message, creation.Error.Code, transactionToken); return Result.Success(false); }
                Notification notification = creation.Value;
                Result delivery = payload.Channel switch
                {
                    NotificationChannel.InApp => Result.Success(),
                    NotificationChannel.Sms => await sms.SendAsync(new(notification.Id, companyId, notification.RecipientUserId, notification.TemplateCode, notification.Payload), transactionToken),
                    NotificationChannel.Email => await email.SendAsync(new(notification.Id, companyId, notification.RecipientUserId, notification.TemplateCode, notification.Payload), transactionToken),
                    NotificationChannel.Push => await push.SendAsync(new(notification.Id, companyId, notification.RecipientUserId, notification.TemplateCode, notification.Payload), transactionToken),
                    _ => Result.Failure(BackgroundErrors.Invalid)
                };
                if (delivery.IsFailure)
                {
                    if (message.Attempts >= BackgroundRetryPolicy.MaximumAttempts) await MoveToDeadLetterAsync(message, delivery.Error.Code, transactionToken);
                    else message.Retry(delivery.Error.Code, time.GetUtcNow() + BackgroundRetryPolicy.Backoff(message.Attempts));
                    return Result.Success(false);
                }
                db.Add(notification); notification.Sent(time.GetUtcNow()); message.Succeed(time.GetUtcNow()); return Result.Success(true);
            }
            catch (JsonException) { await MoveToDeadLetterAsync(message, "outbox.invalid_payload", transactionToken); return Result.Success(false); }
            catch (Exception)
            {
                if (message.Attempts >= BackgroundRetryPolicy.MaximumAttempts) await MoveToDeadLetterAsync(message, "outbox.handler_failed", transactionToken);
                else message.Retry("outbox.handler_failed", time.GetUtcNow() + BackgroundRetryPolicy.Backoff(message.Attempts));
                return Result.Success(false);
            }
        }, cancellationToken);
        return result.IsSuccess && result.Value;
    }

    private async Task MoveToDeadLetterAsync(OutboxMessage message, string code, CancellationToken cancellationToken)
    {
        message.Dead(code);
        if (message.CompanyId is not Guid companyId) return;
        if (!await db.DeadLetterItems.AnyAsync(x => x.OutboxMessageId == message.Id, cancellationToken)) db.Add(DeadLetterItem.Create(companyId, message.Id, code, time.GetUtcNow()));
    }
}
