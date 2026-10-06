using Leyi.Application.Abstractions.Messaging;
using Leyi.Domain.Common;
using Leyi.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.Messaging;

internal static class MessagingDependencyInjection
{
    /// <summary>
    /// RabbitMQ + MassTransit 8.x with the EF Core transactional (bus) outbox and a consumer inbox on every
    /// receive endpoint. Topology is managed by MassTransit. Consumers are registered by the host through
    /// <paramref name="configureConsumers"/>; none exist yet.
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "RabbitMq:Host is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "RabbitMq:Username is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "RabbitMq:Password is required.")
            .Validate(options => options.EffectiveRetryIntervalsSeconds.All(seconds => seconds > 0), "RabbitMq:RetryIntervalsSeconds must be positive.")
            .ValidateOnStart();

        services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();

            bus.AddEntityFrameworkOutbox<LeyiDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                var retryIntervals = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value.EffectiveRetryIntervalsSeconds;
                if (retryIntervals.Count > 0)
                {
                    endpoint.UseMessageRetry(retry =>
                    {
                        retry.Intervals(retryIntervals.Select(seconds => TimeSpan.FromSeconds(seconds)).ToArray());

                        // Business rule violations are permanent: retrying cannot succeed, so they go straight to _error.
                        retry.Ignore<DomainException>();
                    });
                }

                // Consumer inbox: deduplicates redelivered messages per consumer and commits
                // consumer side effects together with the inbox state.
                endpoint.UseEntityFrameworkOutbox<LeyiDbContext>(context);
            });

            configureConsumers?.Invoke(bus);

            bus.UsingRabbitMq((context, rabbit) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                rabbit.Host(options.Host, options.Port, options.VirtualHost, host =>
                {
                    host.Username(options.Username);
                    host.Password(options.Password);
                });

                rabbit.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();

        return services;
    }
}
