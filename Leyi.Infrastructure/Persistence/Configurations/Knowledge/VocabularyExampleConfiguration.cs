using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Knowledge;

/// <summary>ERD §6.11 VocabularyExamples.</summary>
internal sealed class VocabularyExampleConfiguration : IEntityTypeConfiguration<VocabularyExample>
{
    public void Configure(EntityTypeBuilder<VocabularyExample> builder)
    {
        builder.ToTable("VocabularyExamples");
        builder.HasKey(example => example.Id);
        builder.Property(example => example.Id).ValueGeneratedNever();

        builder.Property(example => example.ChineseText).HasMaxLength(1000);
        builder.Property(example => example.Pinyin).HasMaxLength(1500);
        builder.Property(example => example.MeaningVi).HasMaxLength(1500);
        builder.Property(example => example.MeaningEn).HasMaxLength(1500);
        builder.Property(example => example.AudioUrl).HasMaxLength(1000);
        builder.HasAuditTimestamps(updatedAt: false);
    }
}
