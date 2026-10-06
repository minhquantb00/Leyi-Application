using Leyi.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leyi.Infrastructure.Persistence.Transactions;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly LeyiDbContext dbContext;

    public UnitOfWork(LeyiDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // The SQL Server retrying execution strategy requires user-initiated transactions
        // to run inside the strategy so the whole unit can be retried on transient failures.
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(
            (dbContext, operation),
            static async (_, state, ct) =>
            {
                await using var transaction = await state.dbContext.Database.BeginTransactionAsync(ct);
                var result = await state.operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
