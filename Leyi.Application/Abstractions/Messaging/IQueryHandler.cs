namespace Leyi.Application.Abstractions.Messaging;

/// <summary>
/// Handles a single query type. Invoked directly through DI; there is no mediator.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
