using Leyi.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Leyi.Infrastructure.Caching;

internal static class CachingDependencyInjection
{
    private const string CacheInstanceName = "leyi:";

    /// <summary>
    /// Redis connection and <c>IDistributedCache</c>. Redis is an optimization, not a source of truth:
    /// the connection does not abort when Redis is unavailable, so callers can fall back to SQL Server.
    /// No feature caches are defined yet.
    /// </summary>
    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var connectionString = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.Redis;
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider>((options, serviceProvider) =>
            {
                options.InstanceName = CacheInstanceName;
                options.ConnectionMultiplexerFactory = () =>
                    Task.FromResult(serviceProvider.GetRequiredService<IConnectionMultiplexer>());
            });

        return services;
    }
}
