using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Serializes refresh token session operations per user. Canonical lock order for every operation that changes a
/// user's refresh tokens: the <c>Users</c> row first, then <c>RefreshTokens</c>. Login takes it with its
/// <c>LastLoginAt</c> update (exclusive lock); refresh and logout take an update lock without changing the row.
/// Must be called inside the operation's transaction; the lock is held until commit or rollback.
/// </summary>
internal static class UserSessionLock
{
    /// <returns>False when the user no longer exists (its refresh tokens were deleted with it, I2).</returns>
    public static async Task<bool> AcquireAsync(LeyiDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var locked = await dbContext.Database
            .SqlQuery<int>($"SELECT 1 AS [Value] FROM [dbo].[Users] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
            .ToListAsync(cancellationToken);
        return locked.Count == 1;
    }
}
