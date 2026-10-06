using Leyi.Domain.Curricula;
using Leyi.Domain.LearningProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.LearningProfile;

/// <summary>ERD §6.3 UserLearningPlans (rowversion, C3; one ACTIVE plan per user).</summary>
internal sealed class UserLearningPlanConfiguration : IEntityTypeConfiguration<UserLearningPlan>
{
    public void Configure(EntityTypeBuilder<UserLearningPlan> builder)
    {
        builder.ToTable("UserLearningPlans");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id).ValueGeneratedNever();

        builder.Property(plan => plan.PlanType).HasStringEnum(24);
        builder.Property(plan => plan.Status).HasStringEnum(16);
        builder.Property(plan => plan.PlacementStatus).HasStringEnum(20);
        builder.HasAuditTimestamps();
        builder.HasRowVersion();

        builder.HasUser(plan => plan.UserId, DeleteBehavior.Restrict);

        builder.HasOne<UserLearningGoal>()
            .WithMany()
            .HasForeignKey(plan => plan.GoalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Curriculum>()
            .WithMany()
            .HasForeignKey(plan => plan.CurriculumId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CurriculumLevel>()
            .WithMany()
            .HasForeignKey(plan => plan.CurrentLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(plan => plan.CurrentLessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(plan => new { plan.UserId, plan.Status }).HasDatabaseName("IX_UserLearningPlans_User_Status");

        // Phase 0E: at most one ACTIVE plan per user. The application validates first for a clean error;
        // this filtered unique index is the final integrity guard.
        builder.HasIndex(plan => plan.UserId)
            .HasDatabaseName("UX_UserLearningPlans_User_Active")
            .IsUnique()
            .HasFilter("[Status] = 'ACTIVE'");
    }
}
