using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Lock-order anchor for transactions that must serialize mutable state belonging to the same user: the user's
/// <c>Users</c> row is locked first, then the dependent user-owned tables. Current uses: session operations
/// (<c>Users</c> → <c>RefreshTokens</c>: login takes it with its <c>LastLoginAt</c> update, an exclusive lock; refresh
/// and logout take an update lock without changing the row) and onboarding / learning-profile writes
/// (<c>Users</c> → <c>UserLearningGoals</c> / <c>UserLearningPlans</c> / <c>UserReminders</c>). Features that join
/// such a cross-table transaction keep the same order. Not a general-purpose or distributed lock.
/// Must be called inside the operation's transaction; the lock is held until commit or rollback.
/// </summary>
internal static class UserSessionLock
{
    /// <returns>False when the user no longer exists (its refresh tokens are deleted with it, I2).</returns>
    public static async Task<bool> AcquireAsync(LeyiDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var locked = await dbContext.Database
            .SqlQuery<int>($"SELECT 1 AS [Value] FROM [dbo].[Users] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
            .ToListAsync(cancellationToken);
        return locked.Count == 1;
    }
}
