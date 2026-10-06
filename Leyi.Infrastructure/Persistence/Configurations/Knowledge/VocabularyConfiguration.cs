using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Knowledge;

/// <summary>ERD §6.9 Vocabularies with examples (§6.11) and collocations (§6.12) as CASCADE children.</summary>
internal sealed class VocabularyConfiguration : IEntityTypeConfiguration<Vocabulary>
{
    public void Configure(EntityTypeBuilder<Vocabulary> builder)
    {
        builder.ToTable("Vocabularies", table => table.HasCheckConstraint("CK_Vocabularies_HskLevel", "[HskLevel] IS NULL OR [HskLevel] BETWEEN 1 AND 6"));
        builder.HasKey(vocabulary => vocabulary.KnowledgeUnitId);
        builder.Property(vocabulary => vocabulary.KnowledgeUnitId).ValueGeneratedNever();

        builder.Property(vocabulary => vocabulary.Hanzi).HasMaxLength(100);
        builder.Property(vocabulary => vocabulary.Pinyin).HasMaxLength(200);
        builder.Property(vocabulary => vocabulary.PartOfSpeech).HasMaxLength(50);
        builder.Property(vocabulary => vocabulary.MeaningVi).HasMaxLength(1000);
        builder.Property(vocabulary => vocabulary.MeaningEn).HasMaxLength(1000);
        builder.Property(vocabulary => vocabulary.HanViet).HasMaxLength(200);
        builder.Property(vocabulary => vocabulary.AudioUrl).HasMaxLength(1000);
        builder.HasAuditTimestamps();

        builder.HasMany(vocabulary => vocabulary.Examples)
            .WithOne()
            .HasForeignKey(example => example.VocabularyKnowledgeUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(vocabulary => vocabulary.Examples).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(vocabulary => vocabulary.Collocations)
            .WithOne()
            .HasForeignKey(collocation => collocation.VocabularyKnowledgeUnitId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(vocabulary => vocabulary.Collocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
