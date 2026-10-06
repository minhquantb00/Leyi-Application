using Leyi.Application.Features.Identity.Logout;
using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Logout and logout-all (Logout.md, I1). Each operation is one SQL transaction that takes the authenticated user's
/// <see cref="UserSessionLock"/> first (canonical order <c>Users</c> → <c>RefreshTokens</c>, the owner being known
/// from the access token), then revokes with a conditional bulk update limited to active tokens
/// (<c>RevokedAt IS NULL AND ExpiresAt &gt; now</c>), so reasons recorded earlier (ROTATED, LOGOUT, REUSE_DETECTED,
/// SESSION_LIMIT…) are never overwritten.
/// </summary>
/// <remarks>
/// Both operations are idempotent, so after a failed commit the execution strategy simply re-runs them
/// (<c>verifySucceeded</c> returns false): a revocation that did commit then updates no row and the outcome is the
/// same. Account status is not checked: a disabled account may still end its sessions. A deleted user has no tokens
/// left (CASCADE, I2), so the operation is a no-op.
/// </remarks>
internal sealed partial class RefreshTokenRevoker : IRefreshTokenRevoker
{
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<RefreshTokenRevoker> logger;

    public RefreshTokenRevoker(LeyiDbContext dbContext, ILogger<RefreshTokenRevoker> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task RevokeFamilyAsync(Guid userId, string rawRefreshToken, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawRefreshToken);
        var tokenHash = RefreshTokenSecret.Hash(rawRefreshToken);

        var revoked = await RunIdempotentAsync(
            async ct =>
            {
                if (!await UserSessionLock.AcquireAsync(dbContext, userId, ct))
                {
                    return 0;
                }

                // Owner filter: a token of another user is treated exactly like an unknown token (no-op, no signal).
                // Any state of the presented token identifies its family, including an old rotated or expired token.
                var familyId = await dbContext.RefreshTokens.AsNoTracking()
                    .Where(token => token.TokenHash == tokenHash && token.UserId == userId)
                    .Select(token => (Guid?)token.FamilyId)
                    .SingleOrDefaultAsync(ct);
                if (familyId is null)
                {
                    return 0;
                }

                return await RevokeActiveAsync(
                    dbContext.RefreshTokens.Where(token => token.UserId == userId && token.FamilyId == familyId.Value),
                    RefreshTokenRevokedReason.Logout,
                    nowUtc,
                    ct);
            },
            cancellationToken);

        LogLoggedOut(logger, userId, revoked);
    }

    public async Task RevokeAllAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var revoked = await RunIdempotentAsync(
            async ct => await UserSessionLock.AcquireAsync(dbContext, userId, ct)
                ? await RevokeActiveAsync(dbContext.RefreshTokens.Where(token => token.UserId == userId), RefreshTokenRevokedReason.LogoutAll, nowUtc, ct)
                : 0,
            cancellationToken);

        LogLoggedOutEverywhere(logger, userId, revoked);
    }

    private static Task<int> RevokeActiveAsync(IQueryable<RefreshToken> scope, RefreshTokenRevokedReason reason, DateTime nowUtc, CancellationToken cancellationToken) =>
        scope
            .Where(token => token.RevokedAt == null && token.ExpiresAt > nowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, nowUtc)
                    .SetProperty(token => token.RevokedReason, reason),
                cancellationToken);

    /// <summary>One transaction per attempt; re-run after a failed commit because the operation is idempotent.</summary>
    private async Task<int> RunIdempotentAsync(Func<CancellationToken, Task<int>> operation, CancellationToken cancellationToken)
    {
        var revoked = 0;
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            async ct => revoked = await operation(ct),
            _ => Task.FromResult(false),
            cancellationToken);
        return revoked;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Logout for {UserId}: {RevokedTokenCount} active refresh tokens revoked")]
    private static partial void LogLoggedOut(ILogger logger, Guid userId, int revokedTokenCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Logout from all devices for {UserId}: {RevokedTokenCount} active refresh tokens revoked")]
    private static partial void LogLoggedOutEverywhere(ILogger logger, Guid userId, int revokedTokenCount);
}
