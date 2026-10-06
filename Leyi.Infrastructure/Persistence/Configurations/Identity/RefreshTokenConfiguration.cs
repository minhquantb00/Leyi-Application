using Leyi.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// ERD §6.27 RefreshTokens. Technical/security data: CASCADE from Users (I2); the rotation self-reference is NO ACTION.
/// </summary>
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsUnicode(false);

        builder.Property(token => token.DeviceId)
            .HasMaxLength(200);

        builder.Property(token => token.RevokedReason).HasStringEnum(30);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("UX_RefreshTokens_TokenHash")
            .IsUnique();

        builder.HasIndex(token => new { token.UserId, token.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_User_Expires");

        builder.HasIndex(token => token.FamilyId)
            .HasDatabaseName("IX_RefreshTokens_Family");

        // Cleanup path: delete families whose absolute expiry passed the retention window.
        builder.HasIndex(token => token.FamilyExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_FamilyExpiresAt");
    }
}
