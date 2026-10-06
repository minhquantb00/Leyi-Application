namespace Leyi.Application.Features.Identity.Logout;

/// <summary>
/// Ends device sessions of the authenticated user by revoking refresh tokens (Logout.md, I1). Both operations are
/// state-based idempotent: only active tokens are revoked and tokens revoked earlier keep their original reason.
/// </summary>
public interface IRefreshTokenRevoker
{
    /// <summary>
    /// Revokes the active tokens of the family of <paramref name="rawRefreshToken"/> with <c>LOGOUT</c>, whatever the
    /// state of the presented token itself (an old rotated token still identifies its family). No-op, without telling
    /// the caller, when the token is unknown or belongs to another user.
    /// </summary>
    Task RevokeFamilyAsync(Guid userId, string rawRefreshToken, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Revokes every active token of the user with <c>LOGOUT_ALL</c>.</summary>
    Task RevokeAllAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken);
}
