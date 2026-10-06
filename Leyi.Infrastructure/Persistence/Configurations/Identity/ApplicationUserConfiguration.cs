using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// ERD §6.1: Identity user stored in <c>Users</c>; other Identity tables keep their default AspNet* names.
/// </summary>
internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        // Email uniqueness is enforced on NormalizedEmail by the database as well as by
        // IdentityOptions.User.RequireUniqueEmail. Replaces Identity's non-unique EmailIndex.
        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("UX_Users_NormalizedEmail")
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL");

        builder.Property(user => user.Status)
            .HasConversion<UpperSnakeCaseEnumConverter<UserStatus>>()
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasDefaultValue(UserStatus.Active)
            // Explicit sentinel (EF warning 20601): an Active user is inserted through the 'ACTIVE' default.
            .HasSentinel(UserStatus.Active);

        builder.Property(user => user.PreferredTimezone)
            .HasMaxLength(64)
            .IsUnicode(false)
            .HasDefaultValue(ApplicationUser.DefaultTimezone);

        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
