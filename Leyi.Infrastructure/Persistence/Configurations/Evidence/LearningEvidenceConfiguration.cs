using Leyi.Domain.Evidence;
using Leyi.Domain.Knowledge;
using Leyi.Domain.Sessions;
using Leyi.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Evidence;

/// <summary>
/// ERD §6.22 LearningEvidences: append-only history, every FK RESTRICT (C4). LocalDate/TimeZoneId are snapshots.
/// RecentErrorCount is aggregated from this table (B7), served by the filtered Q0 index.
/// </summary>
internal sealed class LearningEvidenceConfiguration : IEntityTypeConfiguration<LearningEvidence>
{
    public void Configure(EntityTypeBuilder<LearningEvidence> builder)
    {
        builder.ToTable("LearningEvidences", table =>
        {
            table.HasCheckConstraint("CK_LearningEvidences_Quality", "[Quality] BETWEEN 0 AND 4");
            table.HasCheckConstraint("CK_LearningEvidences_EvidenceWeight", "[EvidenceWeight] BETWEEN 0 AND 1");
            table.HasCheckConstraint("CK_LearningEvidences_SessionDiscountFactor", "[SessionDiscountFactor] BETWEEN 0 AND 1");
        });
        builder.HasKey(evidence => evidence.Id);
        builder.Property(evidence => evidence.Id).ValueGeneratedNever();

        builder.Property(evidence => evidence.Dimension).HasStringEnum(20);
        builder.Property(evidence => evidence.SourceType).HasStringEnum(30);
        builder.Property(evidence => evidence.ActivityType).HasStringEnum(40);
        builder.Property(evidence => evidence.HintLevel).HasConversion<HintLevelConverter>().HasMaxLength(20).IsUnicode(false);
        builder.Property(evidence => evidence.EvidenceWeight).HasPrecision(4, 3);
        builder.Property(evidence => evidence.SessionDiscountFactor).HasPrecision(4, 3);
        builder.Property(evidence => evidence.PolicyVersion).IsCode(50);
        builder.Property(evidence => evidence.TimeZoneId).IsCode(LearningEvidence.MaxTimeZoneIdLength);

        builder.HasUser(evidence => evidence.UserId, DeleteBehavior.Restrict);

        builder.HasOne<KnowledgeUnit>()
            .WithMany()
            .HasForeignKey(evidence => evidence.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ActivityAttempt>()
            .WithOne()
            .HasForeignKey<LearningEvidence>(evidence => evidence.ActivityAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evidence => new { evidence.UserId, evidence.KnowledgeUnitId, evidence.CreatedAt })
            .HasDatabaseName("IX_LearningEvidences_User_KU_Created")
            .IsDescending(false, false, true)
            .IncludeProperties(evidence => new { evidence.Dimension, evidence.Quality });

        builder.HasIndex(evidence => new { evidence.UserId, evidence.CreatedAt })
            .HasDatabaseName("IX_LearningEvidences_User_RecentErrors")
            .IncludeProperties(evidence => new { evidence.KnowledgeUnitId, evidence.Dimension })
            .HasFilter("[Quality] = 0");

        builder.HasIndex(evidence => evidence.ActivityAttemptId)
            .HasDatabaseName("UX_LearningEvidences_Attempt")
            .IsUnique()
            .HasFilter("[ActivityAttemptId] IS NOT NULL");
    }
}
