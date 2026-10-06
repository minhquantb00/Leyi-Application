using Leyi.Domain.Knowledge;
using Leyi.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Sessions;

/// <summary>
/// ERD §6.19 SessionActivities. ContentSnapshot is an opaque versioned JSON document (C3).
/// Reasons are pure children (CASCADE); attempts are history (RESTRICT).
/// </summary>
internal sealed class SessionActivityConfiguration : IEntityTypeConfiguration<SessionActivity>
{
    public void Configure(EntityTypeBuilder<SessionActivity> builder)
    {
        builder.ToTable("SessionActivities", table =>
        {
            table.HasCheckConstraint("CK_SessionActivities_PriorityScore", "[PriorityScore] IS NULL OR [PriorityScore] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_SessionActivities_EstimatedDuration", "[EstimatedDurationSeconds] > 0");
            table.HasCheckConstraint("CK_SessionActivities_ContentVersion", "[ContentVersion] >= 1");
            table.HasCheckConstraint("CK_SessionActivities_ContentSnapshot", "ISJSON([ContentSnapshot]) = 1");
        });
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.Id).ValueGeneratedNever();

        builder.Property(activity => activity.ActivityType).HasStringEnum(40);
        builder.Property(activity => activity.TargetDimension).HasStringEnum(20);
        builder.Property(activity => activity.PriorityScore).HasPrecision(6, 2);
        builder.Property(activity => activity.Status).HasStringEnum(20);
        builder.Property(activity => activity.ContentSnapshot).HasColumnType("nvarchar(max)");
        builder.HasAuditTimestamps();

        builder.HasOne<KnowledgeUnit>()
            .WithMany()
            .HasForeignKey(activity => activity.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(activity => activity.Reasons)
            .WithOne()
            .HasForeignKey(reason => reason.SessionActivityId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(activity => activity.Reasons).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(activity => activity.Attempts)
            .WithOne()
            .HasForeignKey(attempt => attempt.SessionActivityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(activity => activity.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(activity => new { activity.LearningSessionId, activity.SortOrder }).HasDatabaseName("UX_SessionActivities_Session_Order").IsUnique();
    }
}
