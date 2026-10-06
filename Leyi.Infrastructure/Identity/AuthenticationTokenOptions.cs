namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Access/refresh token policy (I1), bound to the <c>AuthenticationTokens</c> section and validated at startup.
/// Defaults are the approved V1 values; every component reads them from here instead of hard-coding them.
/// </summary>
public sealed class AuthenticationTokenOptions
{
    public const string SectionName = "AuthenticationTokens";

    /// <summary>JWT access token lifetime. Revocation does not shorten an already issued access token.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Lifetime of each refresh token, renewed on rotation but never beyond the family expiry.</summary>
    public TimeSpan RefreshTokenSlidingLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Absolute lifetime of a token family, counted from login. Rotation never extends it.</summary>
    public TimeSpan RefreshTokenAbsoluteFamilyLifetime { get; set; } = TimeSpan.FromDays(90);

    /// <summary>Active token families (device sessions) per user; a new login beyond this revokes the oldest family.</summary>
    public int MaxActiveFamiliesPerUser { get; set; } = 10;

    /// <summary>How long tokens of an expired family are kept before the cleanup job deletes them.</summary>
    public TimeSpan ExpiredFamilyRetention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>Returns the validation failures (empty when the options are consistent).</summary>
    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();

        if (AccessTokenLifetime <= TimeSpan.Zero)
        {
            failures.Add($"{SectionName}:{nameof(AccessTokenLifetime)} must be positive.");
        }

        if (RefreshTokenSlidingLifetime <= TimeSpan.Zero)
        {
            failures.Add($"{SectionName}:{nameof(RefreshTokenSlidingLifetime)} must be positive.");
        }

        if (RefreshTokenAbsoluteFamilyLifetime < RefreshTokenSlidingLifetime)
        {
            failures.Add($"{SectionName}:{nameof(RefreshTokenAbsoluteFamilyLifetime)} must not be shorter than {nameof(RefreshTokenSlidingLifetime)}.");
        }

        if (AccessTokenLifetime >= RefreshTokenSlidingLifetime)
        {
            failures.Add($"{SectionName}:{nameof(AccessTokenLifetime)} must be shorter than {nameof(RefreshTokenSlidingLifetime)}.");
        }

        if (MaxActiveFamiliesPerUser < 1)
        {
            failures.Add($"{SectionName}:{nameof(MaxActiveFamiliesPerUser)} must be at least 1.");
        }

        if (ExpiredFamilyRetention < TimeSpan.Zero)
        {
            failures.Add($"{SectionName}:{nameof(ExpiredFamilyRetention)} must not be negative.");
        }

        return failures;
    }
}
