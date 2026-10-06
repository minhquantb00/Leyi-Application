namespace Leyi.Application.Abstractions.Messaging;

/// <summary>
/// Publishes integration events through the transactional outbox.
/// The event is stored with the business changes and is sent to the broker only after
/// <c>IUnitOfWork.SaveChangesAsync</c> commits. If the transaction rolls back, nothing is sent.
/// Never use this for core learning state transitions (attempt, evidence, mastery, review schedule).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent;
}
