namespace Leyi.Infrastructure.Identity;

/// <summary>Why a refresh token was revoked (I1). Stored as UPPER_SNAKE text in <c>RefreshTokens.RevokedReason</c>.</summary>
public enum RefreshTokenRevokedReason
{
    /// <summary>Replaced by a new token on a successful refresh.</summary>
    Rotated,

    /// <summary>The family (device session) was logged out.</summary>
    Logout,

    /// <summary>All families of the user were logged out.</summary>
    LogoutAll,

    /// <summary>An already rotated token was presented again; the whole family is revoked.</summary>
    ReuseDetected,

    /// <summary>The oldest family was revoked because the user exceeded the active family limit.</summary>
    SessionLimit,

    /// <summary>Reserved for administrative revocation.</summary>
    AdminRevoked,
}
