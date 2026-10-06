using Leyi.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Progress;

/// <summary>ERD §6.23 DailyLearningStats (UpdatedAt only).</summary>
internal sealed class DailyLearningStatsConfiguration : IEntityTypeConfiguration<DailyLearningStats>
{
    public void Configure(EntityTypeBuilder<DailyLearningStats> builder)
    {
        builder.ToTable("DailyLearningStats");
        builder.HasKey(stats => stats.Id);
        builder.Property(stats => stats.Id).ValueGeneratedNever();
        builder.HasAuditTimestamps(createdAt: false);

        builder.HasUser(stats => stats.UserId, DeleteBehavior.Restrict);

        builder.HasIndex(stats => new { stats.UserId, stats.LocalDate }).HasDatabaseName("UX_DailyLearningStats_User_Date").IsUnique();
    }
}
