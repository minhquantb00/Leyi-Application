namespace Leyi.Application.Abstractions.Persistence;

/// <summary>
/// Commits the changes of the current use case to SQL Server.
/// Integration events published through <c>IIntegrationEventPublisher</c> are written to the
/// transactional outbox and committed by the same call.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes in a single database transaction.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> inside an explicit database transaction that is committed when the
    /// operation completes and rolled back when it throws. Use only when a use case needs more than one
    /// <see cref="SaveChangesAsync"/> call to be atomic.
    /// The operation may be re-executed on transient SQL failures, so it must be safe to run again.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
