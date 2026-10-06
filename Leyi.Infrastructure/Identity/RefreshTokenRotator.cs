using Leyi.Application.Abstractions.Authentication;
using Leyi.Application.Features.Identity.RefreshAccessToken;
using Leyi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Refresh token rotation and reuse detection (RefreshAccessToken.md §4, I1) in one SQL transaction per attempt.
/// </summary>
/// <remarks>
/// <para>
/// Lock order is the same as Login (<see cref="RefreshTokenLoginSessionStore"/>): the owner's <c>Users</c> row first,
/// then <c>RefreshTokens</c>. Refresh takes an update lock on the row (<c>UPDLOCK</c>, held until commit) without
/// changing it, so <c>LastLoginAt</c> keeps its meaning; Login's row update and other refreshes of the same user wait
/// for it. The only statement before the lock is an unlocked read of the presented token to find its owner.
/// </para>
/// <para>
/// The rotation itself is a conditional update (<c>WHERE Id = @id AND RevokedAt IS NULL AND ExpiresAt &gt; @now</c>):
/// the database is the final guard. Zero affected rows means another request changed the token first; its persisted
/// state then decides the outcome (a rotated token is treated as reuse). No grace period.
/// </para>
/// <para>
/// Connection resiliency follows the Login pattern: the successor's raw token, hash and id are created once per
/// logical refresh; the rotation saves with <c>acceptAllChangesOnSuccess: false</c>; after a failed commit
/// <c>verifySucceeded</c> looks for the successor hash in SQL Server, so a rotation that did commit is never re-run
/// (and therefore never mistaken for reuse of the now-rotated token). Reuse revocation is idempotent and may be re-run.
/// </para>
/// </remarks>
internal sealed partial class RefreshTokenRotator : IRefreshTokenRotator
{
    private readonly LeyiDbContext dbContext;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IOptions<AuthenticationTokenOptions> tokenOptions;
    private readonly ILogger<RefreshTokenRotator> logger;

    public RefreshTokenRotator(
        LeyiDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<AuthenticationTokenOptions> tokenOptions,
        ILogger<RefreshTokenRotator> logger)
    {
        this.dbContext = dbContext;
        this.userManager = userManager;
        this.tokenOptions = tokenOptions;
        this.logger = logger;
    }

    public async Task<RefreshTokenRotation> RotateAsync(string rawRefreshToken, DateTime nowUtc, Func<Guid, AccessToken> issueAccessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawRefreshToken);
        ArgumentNullException.ThrowIfNull(issueAccessToken);

        var operation = new RotationOperation(
            PresentedHash: RefreshTokenSecret.Hash(rawRefreshToken),
            SuccessorRawToken: RefreshTokenSecret.Generate(),
            SuccessorId: Guid.CreateVersion7(),
            NowUtc: nowUtc,
            IssueAccessToken: issueAccessToken);
        var successorHash = RefreshTokenSecret.Hash(operation.SuccessorRawToken);
        Attempt? lastAttempt = null;

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteInTransactionAsync(
            async ct =>
            {
                ResetTrackedState();
                lastAttempt = await RunAttemptAsync(operation, successorHash, ct);
            },
            // Runs only after a failed commit; a LINQ query always reads committed state from SQL Server.
            ct => dbContext.RefreshTokens.AsNoTracking().AnyAsync(token => token.TokenHash == successorHash, ct),
            cancellationToken);

        dbContext.ChangeTracker.AcceptAllChanges();

        var attempt = lastAttempt ?? throw new InvalidOperationException("The refresh operation completed without an outcome.");
        Log(attempt);
        return attempt.Rotation;
    }

    private async Task<Attempt> RunAttemptAsync(RotationOperation operation, string successorHash, CancellationToken cancellationToken)
    {
        // Unlocked read: only to learn the owner, whose Users row is locked before any refresh token is touched.
        var ownerId = await dbContext.RefreshTokens.AsNoTracking()
            .Where(token => token.TokenHash == operation.PresentedHash)
            .Select(token => (Guid?)token.UserId)
            .SingleOrDefaultAsync(cancellationToken);
        if (ownerId is null || !await UserSessionLock.AcquireAsync(dbContext, ownerId.Value, cancellationToken))
        {
            return Attempt.Rejected(RefreshTokenRotationStatus.Invalid);
        }

        // Read again under the lock: the state committed by any refresh/login of this user that ran before.
        var presented = await dbContext.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == operation.PresentedHash, cancellationToken);
        if (presented is null)
        {
            return Attempt.Rejected(RefreshTokenRotationStatus.Invalid);
        }

        var rejection = await RejectInactiveTokenAsync(presented, operation.NowUtc, cancellationToken);
        if (rejection is not null)
        {
            return rejection;
        }

        if (!await IsAccountEnabledAsync(presented.UserId))
        {
            return Attempt.Rejected(RefreshTokenRotationStatus.AccountDisabled, presented);
        }

        var accessToken = operation.IssueAccessToken(presented.UserId);

        // In memory on an untracked copy: domain rules (active token, family expiry kept, sliding expiry capped).
        var successor = presented.Rotate(operation.SuccessorId, successorHash, operation.NowUtc, tokenOptions.Value);

        var claimed = await dbContext.RefreshTokens
            .Where(token => token.Id == presented.Id && token.RevokedAt == null && token.ExpiresAt > operation.NowUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, presented.RevokedAt)
                    .SetProperty(token => token.RevokedReason, presented.RevokedReason),
                cancellationToken);
        if (claimed != 1)
        {
            // Another request changed the token after it was read: decide from what is persisted now.
            var persisted = await dbContext.RefreshTokens.AsNoTracking()
                .SingleOrDefaultAsync(token => token.Id == presented.Id, cancellationToken);
            return persisted is null
                ? Attempt.Rejected(RefreshTokenRotationStatus.Invalid)
                : await RejectInactiveTokenAsync(persisted, operation.NowUtc, cancellationToken)
                    ?? Attempt.Rejected(RefreshTokenRotationStatus.Invalid, persisted);
        }

        dbContext.RefreshTokens.Add(successor);

        // Changes stay pending until the transaction is known to be committed (see remarks).
        await dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, cancellationToken);

        // Linked after the successor row exists (self-referencing foreign key).
        await dbContext.RefreshTokens
            .Where(token => token.Id == presented.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedByTokenId, successor.Id), cancellationToken);

        return new Attempt(RefreshTokenRotation.Rotated(accessToken, operation.SuccessorRawToken), presented, 0);
    }

    /// <summary>
    /// Applies <see cref="RefreshTokenStateClassifier"/> (the single precedence rule, used by the normal read and by the
    /// failed-claim re-read): reuse revokes the family's active tokens; revoked → revoked, nothing changed;
    /// expired → invalid. Null when the token is active.
    /// </summary>
    private async Task<Attempt?> RejectInactiveTokenAsync(RefreshToken token, DateTime nowUtc, CancellationToken cancellationToken)
    {
        switch (RefreshTokenStateClassifier.Classify(token, nowUtc))
        {
            case RefreshTokenState.Active:
                return null;

            case RefreshTokenState.ReuseDetected:
                // Only active tokens of this family; tokens revoked earlier keep their reason. Idempotent when re-run.
                var revoked = await dbContext.RefreshTokens
                    .Where(family => family.UserId == token.UserId && family.FamilyId == token.FamilyId && family.RevokedAt == null && family.ExpiresAt > nowUtc)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(family => family.RevokedAt, nowUtc)
                            .SetProperty(family => family.RevokedReason, RefreshTokenRevokedReason.ReuseDetected),
                        cancellationToken);
                return new Attempt(RefreshTokenRotation.Rejected(RefreshTokenRotationStatus.ReuseDetected), token, revoked);

            case RefreshTokenState.Revoked:
                return Attempt.Rejected(RefreshTokenRotationStatus.Revoked, token);

            case RefreshTokenState.Expired:
                return Attempt.Rejected(RefreshTokenRotationStatus.Invalid, token);

            default:
                throw new InvalidOperationException("Unknown refresh token state.");
        }
    }

    /// <summary>Status ACTIVE and not locked out (Identity lockout). Email confirmation is not required (I3).</summary>
    private async Task<bool> IsAccountEnabledAsync(Guid userId)
    {
        // UserManager has no CancellationToken parameters; this is a single primary key lookup.
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is { Status: UserStatus.Active } && !await userManager.IsLockedOutAsync(user);
    }

    /// <summary>Each attempt starts from the database state (a rolled back attempt may have left tracked entities).</summary>
    private void ResetTrackedState()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries()
                     .Where(entry => entry.Entity is RefreshToken or ApplicationUser)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private void Log(Attempt attempt)
    {
        var token = attempt.Token;
        switch (attempt.Rotation.Status)
        {
            case RefreshTokenRotationStatus.Rotated when token is not null:
                LogRotated(logger, token.UserId, token.FamilyId);
                break;
            case RefreshTokenRotationStatus.ReuseDetected when token is not null:
                LogReuseDetected(logger, token.UserId, token.FamilyId, attempt.RevokedTokenCount);
                break;
            case RefreshTokenRotationStatus.Revoked when token is not null:
                LogRevokedTokenPresented(logger, token.UserId, token.FamilyId, token.RevokedReason);
                break;
            case RefreshTokenRotationStatus.AccountDisabled when token is not null:
                LogAccountDisabled(logger, token.UserId);
                break;
            default:
                break;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Refresh token rotated for {UserId} (family {FamilyId})")]
    private static partial void LogRotated(ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh token reuse detected for {UserId}: family {FamilyId} revoked ({RevokedTokenCount} active tokens)")]
    private static partial void LogReuseDetected(ILogger logger, Guid userId, Guid familyId, int revokedTokenCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Revoked refresh token presented for {UserId} (family {FamilyId}, reason {RevokedReason})")]
    private static partial void LogRevokedTokenPresented(ILogger logger, Guid userId, Guid familyId, RefreshTokenRevokedReason? revokedReason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Refresh rejected for disabled or locked-out account {UserId}")]
    private static partial void LogAccountDisabled(ILogger logger, Guid userId);

    private sealed record RotationOperation(
        string PresentedHash,
        string SuccessorRawToken,
        Guid SuccessorId,
        DateTime NowUtc,
        Func<Guid, AccessToken> IssueAccessToken)
    {
        // Holds the successor raw token: never printed.
        private bool PrintMembers(System.Text.StringBuilder builder)
        {
            builder.Append("SuccessorId = ").Append(SuccessorId);
            return true;
        }
    }

    private sealed record Attempt(RefreshTokenRotation Rotation, RefreshToken? Token, int RevokedTokenCount)
    {
        public static Attempt Rejected(RefreshTokenRotationStatus status, RefreshToken? token = null) =>
            new(RefreshTokenRotation.Rejected(status), token, 0);
    }
}
