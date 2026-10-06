using Leyi.Infrastructure.Caching;
using Leyi.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Leyi.Infrastructure.HealthChecks;

internal static class HealthChecksDependencyInjection
{
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Readiness checks for required dependencies. RabbitMQ readiness is reported by the MassTransit bus
    /// health check, which MassTransit registers with the <see cref="HealthCheckTags.Ready"/> tag.
    /// Object storage is not required yet and is not checked.
    /// </summary>
    public static IServiceCollection AddReadinessHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<LeyiDbContext>("sqlserver", tags: [HealthCheckTags.Ready])
            .AddCheck<RedisHealthCheck>("redis", tags: [HealthCheckTags.Ready], timeout: CheckTimeout);

        // Bound the SQL check: the retrying execution strategy must not keep readiness hanging.
        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations.Where(registration => registration.Name == "sqlserver"))
            {
                registration.Timeout = CheckTimeout;
            }
        });

        return services;
    }
}
