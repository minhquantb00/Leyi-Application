using Leyi.Domain.Mastery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Mastery;

/// <summary>
/// ERD §6.17 ReviewSchedules (rowversion). UserId is denormalized for the per-user DueNow / DueToday / ReviewDebt
/// query path (B1); the user FK is RESTRICT so the only cascade path is mastery → schedule.
/// </summary>
internal sealed class ReviewScheduleConfiguration : IEntityTypeConfiguration<ReviewSchedule>
{
    public void Configure(EntityTypeBuilder<ReviewSchedule> builder)
    {
        builder.ToTable("ReviewSchedules", table =>
        {
            table.HasCheckConstraint("CK_ReviewSchedules_ReviewLevel", "[ReviewLevel] BETWEEN 0 AND 7");
            table.HasCheckConstraint("CK_ReviewSchedules_LastResultQuality", "[LastResultQuality] IS NULL OR [LastResultQuality] BETWEEN 0 AND 4");
        });
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id).ValueGeneratedNever();

        builder.Property(schedule => schedule.ReviewLevel)
            .HasConversion(level => level.Value, value => ReviewLevel.From(value))
            .HasColumnType("tinyint");
        builder.HasAuditTimestamps();
        builder.HasRowVersion();

        builder.HasUser(schedule => schedule.UserId, DeleteBehavior.Restrict);

        builder.HasIndex(schedule => schedule.UserKnowledgeMasteryId).HasDatabaseName("UX_ReviewSchedules_Mastery").IsUnique();
        builder.HasIndex(schedule => new { schedule.UserId, schedule.NextReviewAt })
            .HasDatabaseName("IX_ReviewSchedules_User_NextReviewAt")
            .IncludeProperties(schedule => new { schedule.ReviewLevel, schedule.UserKnowledgeMasteryId });
    }
}
