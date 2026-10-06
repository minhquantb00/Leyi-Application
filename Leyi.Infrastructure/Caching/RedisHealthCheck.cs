using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Leyi.Infrastructure.Caching;

internal sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer connection;

    public RedisHealthCheck(IConnectionMultiplexer connection)
    {
        this.connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!connection.IsConnected)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis is not connected.");
        }

        var latency = await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken);
        return HealthCheckResult.Healthy($"Redis ping {latency.TotalMilliseconds:F0} ms.");
    }
}
