namespace Leyi.Application.Abstractions.Messaging;

/// <summary>
/// A versioned, immutable contract published to other processes through RabbitMQ.
/// Must not contain domain entities; only the data consumers need.
/// </summary>
public interface IIntegrationEvent
{
    DateTime OccurredAtUtc { get; }
}
