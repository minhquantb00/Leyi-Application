using Leyi.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Sessions;

/// <summary>
/// ERD §6.18 LearningSessions (aggregate root, rowversion). At most one open session per user is enforced by the
/// filtered unique index UX_LearningSessions_User_Open (C5). The session → activity → attempt → evidence chain is
/// learning history and never cascades (C4).
/// </summary>
internal sealed class LearningSessionConfiguration : IEntityTypeConfiguration<LearningSession>
{
    /// <summary>SQL predicate of the open statuses (GENERATED / IN_PROGRESS / PAUSED).</summary>
    public const string OpenSessionFilter = "[Status] IN ('GENERATED', 'IN_PROGRESS', 'PAUSED')";

    public void Configure(EntityTypeBuilder<LearningSession> builder)
    {
        builder.ToTable("LearningSessions", table =>
        {
            table.HasCheckConstraint("CK_LearningSessions_Durations", "([RequestedDurationSeconds] IS NULL OR [RequestedDurationSeconds] > 0) AND ([EffectiveBudgetSeconds] IS NULL OR [EffectiveBudgetSeconds] > 0)");
            table.HasCheckConstraint("CK_LearningSessions_Counters", "[ActualDurationSeconds] >= 0 AND [EvaluatedActivityCount] >= 0");
        });
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();

        builder.Property(session => session.SessionType).HasStringEnum(30);
        builder.Property(session => session.RequestedSkill).HasStringEnum(20);
        builder.Property(session => session.Status).HasStringEnum(20);
        builder.Property(session => session.GenerationVersion).IsCode(50);
        builder.HasAuditTimestamps();
        builder.HasRowVersion();

        builder.HasUser(session => session.UserId, DeleteBehavior.Restrict);

        builder.HasMany(session => session.Activities)
            .WithOne()
            .HasForeignKey(activity => activity.LearningSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(session => session.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(nameof(LearningSession.UserId), ConfigurationExtensions.CreatedAt)
            .HasDatabaseName("IX_LearningSessions_User_CreatedAt")
            .IsDescending(false, true)
            .IncludeProperties(nameof(LearningSession.Status), nameof(LearningSession.EvaluatedActivityCount));

        builder.HasIndex(session => session.UserId)
            .HasDatabaseName("UX_LearningSessions_User_Open")
            .IsUnique()
            .HasFilter(OpenSessionFilter);
    }
}
