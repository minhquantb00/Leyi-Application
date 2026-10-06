using Leyi.Domain.LearningProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.LearningProfile;

/// <summary>ERD §6.2 UserLearningGoals. One active goal per user (filtered unique).</summary>
internal sealed class UserLearningGoalConfiguration : IEntityTypeConfiguration<UserLearningGoal>
{
    public void Configure(EntityTypeBuilder<UserLearningGoal> builder)
    {
        builder.ToTable("UserLearningGoals", table =>
        {
            table.HasCheckConstraint("CK_UserLearningGoals_TargetHSKLevel", "[TargetHSKLevel] IS NULL OR [TargetHSKLevel] BETWEEN 1 AND 6");
            table.HasCheckConstraint("CK_UserLearningGoals_DailyStudyMinutes", "[DailyStudyMinutes] BETWEEN 5 AND 120");
        });
        builder.HasKey(goal => goal.Id);
        builder.Property(goal => goal.Id).ValueGeneratedNever();

        builder.Property(goal => goal.PrimaryGoal).HasStringEnum(32);
        builder.Property(goal => goal.SelfReportedLevel).HasStringEnum(20);
        builder.Property(goal => goal.TargetHskLevel).HasColumnName("TargetHSKLevel");
        builder.HasAuditTimestamps();

        builder.HasUser(goal => goal.UserId, DeleteBehavior.Restrict);

        builder.HasIndex(goal => goal.UserId)
            .HasDatabaseName("UX_UserLearningGoals_Active")
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
