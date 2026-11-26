using BARANG.Core.Application.Background;
using BARANG.Core.Domain.Background;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class BackgroundFoundationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OutboxRetryUsesBoundedExponentialBackoff()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), BackgroundRetryPolicy.Backoff(1));
        Assert.Equal(TimeSpan.FromMinutes(8), BackgroundRetryPolicy.Backoff(4));
        Assert.Equal(TimeSpan.FromMinutes(30), BackgroundRetryPolicy.Backoff(99));
        Assert.Equal(5, BackgroundRetryPolicy.MaximumAttempts);
    }

    [Fact]
    public void OutboxLifecyclePreservesIdentityAndAttemptCount()
    {
        Guid company = Guid.NewGuid();
        OutboxMessage message = OutboxMessage.Create(company, "notification.requested", "{}", "correlation-1", Now).Value;
        Guid id = message.Id;
        message.Start(Now);
        message.Retry("provider.transient", Now.AddMinutes(1));
        message.Start(Now.AddMinutes(1));
        message.Succeed(Now.AddMinutes(1));
        Assert.Equal(id, message.Id);
        Assert.Equal(2, message.Attempts);
        Assert.Equal(OutboxStatus.Processed, message.Status);
        Assert.NotNull(message.ProcessedAtUtc);
    }

    [Fact]
    public void InvalidOutboxPayloadIsRejected()
    {
        Assert.True(OutboxMessage.Create(Guid.NewGuid(), "x", "{}", "c", Now).IsFailure);
        Assert.True(OutboxMessage.Create(Guid.NewGuid(), "valid.event", new string('x', 8001), "c", Now).IsFailure);
    }

    [Fact]
    public void NotificationReadRequiresRecipientAndFreshVersion()
    {
        Guid recipient = Guid.NewGuid();
        Notification notification = Notification.Create(Guid.NewGuid(), recipient, NotificationChannel.InApp, "order.ready", "{}", Now).Value;
        Assert.True(notification.Read(Guid.NewGuid(), 1, Now).IsFailure);
        Assert.True(notification.Read(recipient, 0, Now).IsFailure);
        Assert.True(notification.Read(recipient, 1, Now).IsSuccess);
        Assert.NotNull(notification.ReadAtUtc);
    }

    [Fact]
    public void CacheKeysAreCompanyAndCultureIsolated()
    {
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        string fa = new CacheKey(first, "fa-IR", "lookup:order.status").ToString();
        Assert.NotEqual(fa, new CacheKey(second, "fa-IR", "lookup:order.status").ToString());
        Assert.NotEqual(fa, new CacheKey(first, "en-US", "lookup:order.status").ToString());
        Assert.Contains(first.ToString("N"), fa, StringComparison.Ordinal);
    }
}
