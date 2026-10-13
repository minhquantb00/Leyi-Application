using Leyi.Infrastructure.Caching;
using Leyi.Infrastructure.Configuration;
using Leyi.Infrastructure.HealthChecks;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.LearningProfile;
using Leyi.Infrastructure.Messaging;
using Leyi.Infrastructure.Persistence;
using Leyi.Infrastructure.Storage;
using Leyi.Infrastructure.Today;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Leyi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure adapters shared by the API and Worker hosts:
    /// SQL Server/EF Core, Identity persistence, Redis, RabbitMQ/MassTransit with EF outbox/inbox,
    /// object storage options and readiness health checks.
    /// </summary>
    /// <param name="configureConsumers">Host-specific MassTransit consumer registrations (Worker).</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.SqlServer), "ConnectionStrings:SqlServer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Redis), "ConnectionStrings:Redis is required.")
            .ValidateOnStart();

        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName));

        services.AddPersistence();
        services.AddIdentityFoundation(configuration);
        services.AddLearningProfile();
        services.AddToday();
        services.AddCaching();
        services.AddMessaging(configuration, configureConsumers);
        services.AddReadinessHealthChecks();

        return services;
    }
}
