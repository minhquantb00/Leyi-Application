namespace Leyi.Infrastructure.Identity;

/// <summary>How a presented, existing refresh token is treated by a refresh (RefreshAccessToken.md §4, I1).</summary>
public enum RefreshTokenState
{
    /// <summary>Not revoked, not expired, family not expired: eligible for rotation.</summary>
    Active,

    /// <summary>Revoked as ROTATED and presented again: reuse, whatever its own expiry.</summary>
    ReuseDetected,

    /// <summary>Revoked for any other reason (logout, logout-all, session limit, earlier reuse, admin).</summary>
    Revoked,

    /// <summary>Not revoked but past its own expiry or its family's absolute expiry.</summary>
    Expired,
}

/// <summary>
/// The single precedence rule for refresh token classification. A token revoked as ROTATED is always reuse, even after
/// its own expiry: rotated tokens are kept until the family expires precisely so that reuse detection keeps working
/// (ERD §6.27). Unknown tokens are handled by the caller (they have no state to classify).
/// </summary>
public static class RefreshTokenStateClassifier
{
    public static RefreshTokenState Classify(RefreshToken token, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.RevokedReason == RefreshTokenRevokedReason.Rotated)
        {
            return RefreshTokenState.ReuseDetected;
        }

        if (token.IsRevoked)
        {
            return RefreshTokenState.Revoked;
        }

        if (token.IsExpired(nowUtc))
        {
            return RefreshTokenState.Expired;
        }

        // Implied by ExpiresAt <= FamilyExpiresAt; kept explicit so the absolute family lifetime is visibly enforced.
        return nowUtc >= token.FamilyExpiresAt ? RefreshTokenState.Expired : RefreshTokenState.Active;
    }
}
