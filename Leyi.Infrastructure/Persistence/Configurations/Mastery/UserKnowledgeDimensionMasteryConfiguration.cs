using Leyi.Domain.Mastery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Mastery;

/// <summary>ERD §6.16 UserKnowledgeDimensionMasteries. No RecentErrorCount column (B7: computed from evidence).</summary>
internal sealed class UserKnowledgeDimensionMasteryConfiguration : IEntityTypeConfiguration<UserKnowledgeDimensionMastery>
{
    public void Configure(EntityTypeBuilder<UserKnowledgeDimensionMastery> builder)
    {
        builder.ToTable("UserKnowledgeDimensionMasteries", table =>
        {
            table.HasCheckConstraint("CK_UKDM_Score", "[Score] BETWEEN 0 AND 100");
            table.HasCheckConstraint("CK_UKDM_EvidenceCount", "[EvidenceCount] >= 0");
        });
        builder.HasKey(dimension => dimension.Id);
        builder.Property(dimension => dimension.Id).ValueGeneratedNever();

        builder.Property(dimension => dimension.Dimension).HasStringEnum(20);
        builder.Property(dimension => dimension.Score).HasPrecision(5, 2);
        builder.HasAuditTimestamps();

        builder.HasIndex(dimension => new { dimension.UserKnowledgeMasteryId, dimension.Dimension }).HasDatabaseName("UX_UKDM_Mastery_Dimension").IsUnique();
    }
}
