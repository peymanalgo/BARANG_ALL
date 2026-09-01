using BARANG.Core.Application.Background;using BARANG.Core.Domain.Common;
namespace BARANG.Core.Infrastructure.Background;
public sealed class LocalNotificationSender:ISmsSender,IEmailSender,IPushSender
{public Task<Result>SendAsync(NotificationDelivery delivery,CancellationToken ct){ct.ThrowIfCancellationRequested();return Task.FromResult(Result.Success());}}
