using Microsoft.Extensions.Diagnostics.HealthChecks;using StackExchange.Redis;
namespace BARANG.Core.Infrastructure.Background;
public sealed class RedisHealthCheck(IConnectionMultiplexer redis):IHealthCheck{public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken){try{cancellationToken.ThrowIfCancellationRequested();await redis.GetDatabase().PingAsync();return HealthCheckResult.Healthy();}catch(Exception e){return HealthCheckResult.Unhealthy("Redis unavailable",e);}}}
