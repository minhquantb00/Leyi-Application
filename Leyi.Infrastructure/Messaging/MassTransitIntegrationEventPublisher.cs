using Leyi.Application.Abstractions.Messaging;
using MassTransit;

namespace Leyi.Infrastructure.Messaging;

/// <summary>
/// Publishes through the scoped MassTransit publish endpoint. With the EF Core bus outbox enabled, the message is
/// added to <see cref="Persistence.LeyiDbContext"/> and delivered by the bus outbox delivery service after commit.
/// </summary>
internal sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        this.publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
