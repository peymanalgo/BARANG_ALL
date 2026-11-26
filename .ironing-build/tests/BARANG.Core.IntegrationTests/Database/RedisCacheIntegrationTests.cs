using BARANG.Core.Application.Background;
using BARANG.Core.Infrastructure.Background;
using StackExchange.Redis;
using Xunit;

namespace BARANG.Core.IntegrationTests.Database;

public sealed class RedisCacheIntegrationTests
{
    private sealed record Probe(string Value);

    [Fact]
    public async Task RedisCacheRoundTripIsolationAndInvalidationAreReal()
    {
        string connectionString = Environment.GetEnvironmentVariable("BARANG_TEST_REDIS_CONNECTION") ?? "localhost:6379,abortConnect=false";
        await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        RedisApplicationCache cache = new(connection);
        Guid first = Guid.NewGuid(), second = Guid.NewGuid();
        CacheKey firstKey = new(first, "fa-IR", "phase19:probe");
        CacheKey secondKey = new(second, "fa-IR", "phase19:probe");
        await cache.SetAsync(firstKey, new Probe("first"), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        await cache.SetAsync(secondKey, new Probe("second"), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
        Assert.Equal("first", (await cache.GetAsync<Probe>(firstKey, TestContext.Current.CancellationToken))?.Value);
        Assert.Equal("second", (await cache.GetAsync<Probe>(secondKey, TestContext.Current.CancellationToken))?.Value);
        await cache.RemoveAsync(firstKey, TestContext.Current.CancellationToken);
        Assert.Null(await cache.GetAsync<Probe>(firstKey, TestContext.Current.CancellationToken));
        Assert.Equal("second", (await cache.GetAsync<Probe>(secondKey, TestContext.Current.CancellationToken))?.Value);
        await cache.RemoveAsync(secondKey, TestContext.Current.CancellationToken);
    }
}
