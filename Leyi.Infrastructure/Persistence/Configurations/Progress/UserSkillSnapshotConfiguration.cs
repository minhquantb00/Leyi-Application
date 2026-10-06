using Leyi.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Progress;

/// <summary>ERD §6.23.1 UserSkillSnapshots (UpdatedAt only).</summary>
internal sealed class UserSkillSnapshotConfiguration : IEntityTypeConfiguration<UserSkillSnapshot>
{
    public void Configure(EntityTypeBuilder<UserSkillSnapshot> builder)
    {
        builder.ToTable("UserSkillSnapshots", table => table.HasCheckConstraint("CK_UserSkillSnapshots_Score", "[Score] BETWEEN 0 AND 100"));
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Id).ValueGeneratedNever();

        builder.Property(snapshot => snapshot.Dimension).HasStringEnum(20);
        builder.Property(snapshot => snapshot.Score).HasPrecision(5, 2);
        builder.HasAuditTimestamps(createdAt: false);

        builder.HasUser(snapshot => snapshot.UserId, DeleteBehavior.Restrict);

        builder.HasIndex(snapshot => new { snapshot.UserId, snapshot.LocalDate, snapshot.Dimension })
            .HasDatabaseName("UX_UserSkillSnapshots_User_Date_Dimension")
            .IsUnique();
    }
}
