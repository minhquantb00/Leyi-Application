namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Persisted refresh token (ERD §6.27, I1). Only the SHA-256 hash of the 256-bit raw token is stored; the raw token
/// is never persisted or logged. Tokens issued from one login share a family whose absolute expiry
/// (<see cref="FamilyExpiresAt"/>) is fixed at login and copied unchanged to every rotated token.
/// Issuing, hashing and JWT handling belong to the authentication features, not to this model.
/// </summary>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the raw token (lowercase hex). The raw token is never persisted or logged.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Rotation chain started by one login (one device session).</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>Absolute expiry of the family: login time + absolute family lifetime. Never reset by rotation.</summary>
    public DateTime FamilyExpiresAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>Client installation id, informational only (never used for security decisions).</summary>
    public string? DeviceId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>min(issued + sliding lifetime, <see cref="FamilyExpiresAt"/>).</summary>
    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public RefreshTokenRevokedReason? RevokedReason { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAt;

    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    /// <summary>Login: starts a new family whose absolute expiry is fixed now.</summary>
    public static RefreshToken StartFamily(Guid userId, string tokenHash, string? deviceId, DateTime nowUtc, AuthenticationTokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        EnsureUtc(nowUtc);

        return Issue(Guid.CreateVersion7(), userId, tokenHash, Guid.CreateVersion7(), nowUtc + options.RefreshTokenAbsoluteFamilyLifetime, deviceId, nowUtc, options);
    }

    /// <summary>
    /// Successful refresh: revokes this token as ROTATED and returns its successor in the same family. The successor
    /// keeps <see cref="FamilyExpiresAt"/>; its expiry is min(now + sliding lifetime, family expiry).
    /// </summary>
    public RefreshToken Rotate(string newTokenHash, DateTime nowUtc, AuthenticationTokenOptions options) =>
        Rotate(Guid.CreateVersion7(), newTokenHash, nowUtc, options);

    /// <summary>
    /// <see cref="Rotate(string, DateTime, AuthenticationTokenOptions)"/> with a successor id chosen by the caller, so a
    /// retried refresh operation persists the same successor.
    /// </summary>
    public RefreshToken Rotate(Guid successorId, string newTokenHash, DateTime nowUtc, AuthenticationTokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (successorId == Guid.Empty)
        {
            throw new ArgumentException("Successor id is required.", nameof(successorId));
        }

        if (!IsActive(nowUtc))
        {
            throw new InvalidOperationException("Only an active refresh token can be rotated.");
        }

        var successor = Issue(successorId, UserId, newTokenHash, FamilyId, FamilyExpiresAt, DeviceId, nowUtc, options);
        Revoke(RefreshTokenRevokedReason.Rotated, nowUtc);
        ReplacedByTokenId = successor.Id;
        return successor;
    }

    /// <summary>Revokes the token. Idempotent: an already revoked token keeps its original time and reason.</summary>
    public void Revoke(RefreshTokenRevokedReason reason, DateTime nowUtc)
    {
        EnsureUtc(nowUtc);

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined revocation reason.");
        }

        if (IsRevoked)
        {
            return;
        }

        RevokedAt = nowUtc;
        RevokedReason = reason;
    }

    private static RefreshToken Issue(Guid id, Guid userId, string tokenHash, Guid familyId, DateTime familyExpiresAt, string? deviceId, DateTime nowUtc, AuthenticationTokenOptions options)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        EnsureUtc(nowUtc);

        if (familyExpiresAt <= nowUtc)
        {
            throw new InvalidOperationException("The token family has expired.");
        }

        var slidingExpiry = nowUtc + options.RefreshTokenSlidingLifetime;

        return new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId,
            FamilyExpiresAt = familyExpiresAt,
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim(),
            CreatedAt = nowUtc,
            ExpiresAt = slidingExpiry < familyExpiresAt ? slidingExpiry : familyExpiresAt,
        };
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamps must be UTC.", nameof(value));
        }
    }
}
