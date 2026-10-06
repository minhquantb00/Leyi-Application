namespace Leyi.Application.Abstractions.Messaging;

/// <summary>
/// Handles a single command type. Invoked directly through DI; there is no mediator.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
