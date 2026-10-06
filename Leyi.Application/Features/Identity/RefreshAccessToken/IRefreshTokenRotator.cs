using System.Text;
using Leyi.Application.Abstractions.Authentication;

namespace Leyi.Application.Features.Identity.RefreshAccessToken;

/// <summary>
/// Validates a presented refresh token and, when it is active and its owner may sign in, rotates it within its family
/// in one transaction (RefreshAccessToken.md §4, I1). Reuse of a rotated token revokes the active tokens of that family.
/// </summary>
public interface IRefreshTokenRotator
{
    /// <param name="rawRefreshToken">The token presented by the client; only its SHA-256 hash is used for lookup.</param>
    /// <param name="nowUtc">Time of the refresh.</param>
    /// <param name="issueAccessToken">
    /// Called with the token owner only after the token and the account were validated and before the rotation is
    /// persisted, so no access token is issued for a rejected refresh and a failed issuance changes nothing.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RefreshTokenRotation> RotateAsync(string rawRefreshToken, DateTime nowUtc, Func<Guid, AccessToken> issueAccessToken, CancellationToken cancellationToken);
}

public enum RefreshTokenRotationStatus
{
    Rotated,

    /// <summary>Unknown or expired token (an expired family always has expired tokens).</summary>
    Invalid,

    /// <summary>Token revoked for a reason other than rotation (logout, session limit, earlier reuse detection...).</summary>
    Revoked,

    /// <summary>A rotated token was presented again; the active tokens of its family were revoked.</summary>
    ReuseDetected,

    /// <summary>Owner status is not active or the account is locked out. Nothing was rotated.</summary>
    AccountDisabled,
}

/// <summary>Outcome of a rotation. Token values are never written by <see cref="object.ToString"/>.</summary>
public sealed record RefreshTokenRotation(RefreshTokenRotationStatus Status, AccessToken? AccessToken, string? RefreshToken)
{
    public static RefreshTokenRotation Rotated(AccessToken accessToken, string refreshToken) =>
        new(RefreshTokenRotationStatus.Rotated, accessToken, refreshToken);

    public static RefreshTokenRotation Rejected(RefreshTokenRotationStatus status) =>
        status == RefreshTokenRotationStatus.Rotated
            ? throw new ArgumentOutOfRangeException(nameof(status), status, "A successful rotation carries a token pair.")
            : new(status, null, null);

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Status = ").Append(Status).Append(", AccessToken = ***, RefreshToken = ***");
        return true;
    }
}
