using Leyi.Domain.Knowledge;
using Leyi.Domain.Mastery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Mastery;

/// <summary>
/// ERD §6.15 UserKnowledgeMasteries (aggregate root, rowversion). NEW is never persisted (CHECK constraint).
/// Dimensions and the review schedule are pure children (CASCADE). Learning history references are RESTRICT (C4).
/// </summary>
internal sealed class UserKnowledgeMasteryConfiguration : IEntityTypeConfiguration<UserKnowledgeMastery>
{
    public void Configure(EntityTypeBuilder<UserKnowledgeMastery> builder)
    {
        builder.ToTable("UserKnowledgeMasteries", table =>
        {
            table.HasCheckConstraint("CK_UserKnowledgeMasteries_OverallMastery", "[OverallMastery] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_UserKnowledgeMasteries_KnowledgeState", "[KnowledgeState] IN ('LEARNING', 'REVIEWING', 'WEAK', 'STRONG', 'MASTERED')");
            table.HasCheckConstraint("CK_UserKnowledgeMasteries_Counts", "[TotalEvidenceCount] >= 0 AND [EvidenceDayCount] >= 0 AND [EvidenceDayCount] <= [TotalEvidenceCount]");
        });
        builder.HasKey(mastery => mastery.Id);
        builder.Property(mastery => mastery.Id).ValueGeneratedNever();

        builder.Property(mastery => mastery.KnowledgeState).HasStringEnum(20);
        builder.Property(mastery => mastery.OverallMastery).HasPrecision(5, 2);
        builder.Property(mastery => mastery.ConfidenceLevel).HasStringEnum(12);
        builder.HasAuditTimestamps();
        builder.HasRowVersion();

        builder.HasUser(mastery => mastery.UserId, DeleteBehavior.Restrict);

        builder.HasOne<KnowledgeUnit>()
            .WithMany()
            .HasForeignKey(mastery => mastery.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(mastery => mastery.Dimensions)
            .WithOne()
            .HasForeignKey(dimension => dimension.UserKnowledgeMasteryId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(mastery => mastery.Dimensions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(mastery => mastery.ReviewSchedule)
            .WithOne()
            .HasForeignKey<ReviewSchedule>(schedule => schedule.UserKnowledgeMasteryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(mastery => new { mastery.UserId, mastery.KnowledgeUnitId }).HasDatabaseName("UX_UserKnowledgeMasteries_User_KU").IsUnique();
        builder.HasIndex(mastery => new { mastery.UserId, mastery.KnowledgeState }).HasDatabaseName("IX_UserKnowledgeMasteries_User_State");
        builder.HasIndex(mastery => new { mastery.UserId, mastery.LastErrorAt }).HasDatabaseName("IX_UserKnowledgeMasteries_User_LastErrorAt");
    }
}
