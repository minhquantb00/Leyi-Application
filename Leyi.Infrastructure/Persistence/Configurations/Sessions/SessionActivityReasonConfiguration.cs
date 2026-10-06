using Leyi.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Sessions;

/// <summary>ERD §6.20 SessionActivityReasons.</summary>
internal sealed class SessionActivityReasonConfiguration : IEntityTypeConfiguration<SessionActivityReason>
{
    public void Configure(EntityTypeBuilder<SessionActivityReason> builder)
    {
        builder.ToTable("SessionActivityReasons");
        builder.HasKey(reason => reason.Id);
        builder.Property(reason => reason.Id).ValueGeneratedNever();

        builder.Property(reason => reason.Reason).HasStringEnum(30);
        builder.Property(reason => reason.ScoreContribution).HasPrecision(6, 2);
        builder.HasAuditTimestamps(updatedAt: false);
    }
}
