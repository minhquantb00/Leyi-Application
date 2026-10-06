using Leyi.Application.Features.Identity.Login;
using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Starts a refresh token family for a login in one SQL transaction (Login.md §6):
/// <c>Users.LastLoginAt</c> update, revocation of the oldest active families beyond
/// <see cref="AuthenticationTokenOptions.MaxActiveFamiliesPerUser"/> (<c>SESSION_LIMIT</c>) and the new token row.
/// The <c>Users</c> row update comes first so that concurrent logins of the same user are serialized by its row lock
/// and each one counts the families committed by the previous one.
/// </summary>
/// <remarks>
/// Connection resiliency follows the EF Core pattern for commit failures: the raw token and its hash are created once
/// per logical login; each attempt saves with <c>acceptAllChangesOnSuccess: false</c> so a rolled back attempt can be
/// re-run with all pending changes; after a failed commit, <c>verifySucceeded</c> checks SQL Server for the token hash
/// so an attempt that actually committed is not re-run; changes are accepted only after the whole operation succeeds.
/// </remarks>
internal sealed partial class RefreshTokenLoginSessionStore : ILoginSessionStore
{
    private readonly LeyiDbContext dbContext;
    private readonly IOptions<AuthenticationTokenOptions> tokenOptions;
    private readonly ILogger<RefreshTokenLoginSessionStore> logger;

    public RefreshTokenLoginSessionStore(LeyiDbContext dbContext, IOptions<AuthenticationTokenOptions> tokenOptions, ILogger<RefreshTokenLoginSessionStore> logger)
    {
        this.dbContext = dbContext;
        this.tokenOptions = tokenOptions;
        this.logger = logger;
    }

    public async Task<string> StartSessionAsync(Guid userId, string? deviceId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var options = tokenOptions.Value;
        var rawToken = RefreshTokenSecret.Generate();
        var tokenHash = RefreshTokenSecret.Hash(rawToken);
        var refreshToken = RefreshToken.StartFamily(userId, tokenHash, deviceId, nowUtc, options);
        var revokedFamilies = 0;

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            async ct =>
            {
                PrepareAttempt(refreshToken);

                var updatedUsers = await dbContext.Users
                    .Where(user => user.Id == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastLoginAt, nowUtc), ct);
                if (updatedUsers != 1)
                {
                    throw new InvalidOperationException("The authenticated user no longer exists.");
                }

                revokedFamilies = await RevokeFamiliesBeyondLimitAsync(userId, nowUtc, options.MaxActiveFamiliesPerUser, ct);

                // Keep the changes pending until the transaction is known to be committed (see remarks).
                await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct);
            },
            // Runs only after a failed commit; a LINQ query always reads committed state from SQL Server.
            ct => dbContext.RefreshTokens.AsNoTracking().AnyAsync(token => token.TokenHash == tokenHash, ct),
            cancellationToken);

        dbContext.ChangeTracker.AcceptAllChanges();

        LogSessionStarted(logger, userId, refreshToken.FamilyId, revokedFamilies);
        return rawToken;
    }

    /// <summary>
    /// Makes every attempt start from the database state: the new token is pending insert, and refresh tokens loaded
    /// or revoked by a rolled back attempt are detached so the session limit is evaluated again on fresh rows
    /// (another login may have committed in between).
    /// </summary>
    private void PrepareAttempt(RefreshToken newToken)
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<RefreshToken>().Where(entry => entry.Entity != newToken).ToList())
        {
            entry.State = EntityState.Detached;
        }

        if (dbContext.Entry(newToken).State != EntityState.Added)
        {
            dbContext.RefreshTokens.Add(newToken);
        }
    }

    /// <summary>
    /// Leaves room for the new family: revokes every token of the oldest active families so that, after the insert,
    /// the user has at most <paramref name="maxActiveFamilies"/> active families. Families are counted, not token rows
    /// (a family keeps its rotated rows); a family is active while it has a token that is neither revoked nor expired.
    /// </summary>
    private async Task<int> RevokeFamiliesBeyondLimitAsync(Guid userId, DateTime nowUtc, int maxActiveFamilies, CancellationToken cancellationToken)
    {
        var activeFamilyIds = dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > nowUtc)
            .Select(token => token.FamilyId);

        var activeFamilies = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && activeFamilyIds.Contains(token.FamilyId))
            .GroupBy(token => token.FamilyId)
            .Select(family => new { FamilyId = family.Key, StartedAt = family.Min(token => token.CreatedAt) })
            .OrderBy(family => family.StartedAt)
            .ThenBy(family => family.FamilyId)
            .ToListAsync(cancellationToken);

        var excess = activeFamilies.Count - (maxActiveFamilies - 1);
        if (excess <= 0)
        {
            return 0;
        }

        var familiesToRevoke = activeFamilies.Take(excess).Select(family => family.FamilyId).ToList();
        var tokensToRevoke = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && familiesToRevoke.Contains(token.FamilyId) && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokensToRevoke)
        {
            token.Revoke(RefreshTokenRevokedReason.SessionLimit, nowUtc);
        }

        return familiesToRevoke.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Login session started for {UserId} (family {FamilyId}); {RevokedFamilyCount} oldest families revoked by the session limit")]
    private static partial void LogSessionStarted(ILogger logger, Guid userId, Guid familyId, int revokedFamilyCount);
}
