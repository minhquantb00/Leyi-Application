using Leyi.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity;

namespace Leyi.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user mapped to the <c>Users</c> table (ERD §6.1).
/// Contains only the Leyi fields approved in the ERD; learning data lives in its own aggregates.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>, IHasTimestamps
{
    public const string DefaultTimezone = "Asia/Ho_Chi_Minh";

    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>IANA timezone used for daily goal/reminder boundaries.</summary>
    public string PreferredTimezone { get; set; } = DefaultTimezone;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }
}
