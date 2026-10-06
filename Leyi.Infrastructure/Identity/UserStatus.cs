namespace Leyi.Infrastructure.Identity;

/// <summary>
/// Account lifecycle status (ERD §6.1). Stored as ACTIVE / SUSPENDED / DELETED.
/// Independent from Identity lockout, which protects against brute force.
/// </summary>
public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Deleted = 3,
}
