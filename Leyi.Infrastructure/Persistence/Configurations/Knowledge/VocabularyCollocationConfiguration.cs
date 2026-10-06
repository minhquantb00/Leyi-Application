using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Knowledge;

/// <summary>ERD §6.12 VocabularyCollocations.</summary>
internal sealed class VocabularyCollocationConfiguration : IEntityTypeConfiguration<VocabularyCollocation>
{
    public void Configure(EntityTypeBuilder<VocabularyCollocation> builder)
    {
        builder.ToTable("VocabularyCollocations");
        builder.HasKey(collocation => collocation.Id);
        builder.Property(collocation => collocation.Id).ValueGeneratedNever();

        builder.Property(collocation => collocation.Text).HasMaxLength(300);
        builder.Property(collocation => collocation.Pinyin).HasMaxLength(500);
        builder.Property(collocation => collocation.MeaningVi).HasMaxLength(500);
        builder.HasAuditTimestamps(updatedAt: false);
    }
}
