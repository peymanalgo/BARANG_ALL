using BARANG.Core.Application.Idempotency;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class IdempotencyTests
{
    [Fact]
    public void EqualKeysHaveValueSemantics()
    {
        IdempotencyKey first = new("mobile:create:12345");
        IdempotencyKey second = new("mobile:create:12345");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("contains space")]
    [InlineData("کلید-نامعتبر")]
    public void KeyRejectsUnsafeValues(string value)
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyKey(value));
    }

    [Fact]
    public void RequestCapturesScopeAndCanonicalExpiration()
    {
        Guid companyId = Guid.NewGuid();
        DateTimeOffset expiresAt = new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        IdempotencyScope scope = new("core.operation.create", "actor-7", companyId, "client-2");

        IdempotencyRequest request = new(new IdempotencyKey("request-key-123"), scope, expiresAt);

        Assert.Equal(companyId, request.Scope.CompanyId);
        Assert.Equal(TimeSpan.Zero, request.ExpiresAtUtc.Offset);
    }

    [Fact]
    public void RequestRejectsNonCanonicalExpirationOffset()
    {
        IdempotencyScope scope = new("core.operation.create", "actor-7");
        DateTimeOffset localTimestamp = new(2026, 8, 26, 12, 0, 0, TimeSpan.FromHours(3.5));

        Assert.Throws<ArgumentException>(() => new IdempotencyRequest(
            new IdempotencyKey("request-key-123"),
            scope,
            localTimestamp));
    }

    [Fact]
    public void ScopeRejectsLocalizedOperationIdentifier()
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyScope("عملیات.ایجاد", "actor-7"));
    }
}
