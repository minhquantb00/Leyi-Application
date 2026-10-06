using Leyi.Domain.Sessions;
using Leyi.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Sessions;

/// <summary>ERD §6.21 ActivityAttempts. Attempt numbers are unique per activity.</summary>
internal sealed class ActivityAttemptConfiguration : IEntityTypeConfiguration<ActivityAttempt>
{
    public void Configure(EntityTypeBuilder<ActivityAttempt> builder)
    {
        builder.ToTable("ActivityAttempts", table =>
        {
            table.HasCheckConstraint("CK_ActivityAttempts_AttemptNumber", "[AttemptNumber] >= 1");
            table.HasCheckConstraint("CK_ActivityAttempts_Quality", "[Quality] IS NULL OR [Quality] BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_ActivityAttempts_AudioReplayCount", "[AudioReplayCount] IS NULL OR [AudioReplayCount] BETWEEN 0 AND 20");
            table.HasCheckConstraint("CK_ActivityAttempts_ResponseTimeMs", "[ResponseTimeMs] IS NULL OR [ResponseTimeMs] >= 0");
        });
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();

        builder.Property(attempt => attempt.HintLevel).HasConversion<HintLevelConverter>().HasMaxLength(20).IsUnicode(false);

        builder.HasIndex(attempt => new { attempt.SessionActivityId, attempt.AttemptNumber }).HasDatabaseName("UX_ActivityAttempts_Activity_Attempt").IsUnique();
    }
}
