namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Leyi V1 brute-force protection policy (Login.md §4). Configured explicitly so that it never depends on ASP.NET
/// Core Identity defaults. Not operational configuration: changing it is a security decision.
/// </summary>
public static class IdentitySecurityDefaults
{
    /// <summary>Lockout applies to every account, including newly registered ones.</summary>
    public const bool LockoutAllowedForNewUsers = true;

    /// <summary>Consecutive failed password attempts that lock the account.</summary>
    public const int MaxFailedAccessAttempts = 5;

    public const int LockoutMinutes = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(LockoutMinutes);
}
