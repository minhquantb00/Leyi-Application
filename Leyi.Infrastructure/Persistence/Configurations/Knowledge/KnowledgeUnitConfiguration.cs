using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Knowledge;

/// <summary>ERD §6.8 KnowledgeUnits with table-per-type subtypes Vocabularies / GrammarPatterns (PK = FK, CASCADE).</summary>
internal sealed class KnowledgeUnitConfiguration : IEntityTypeConfiguration<KnowledgeUnit>
{
    public void Configure(EntityTypeBuilder<KnowledgeUnit> builder)
    {
        builder.ToTable("KnowledgeUnits", table => table.HasCheckConstraint("CK_KnowledgeUnits_ContentVersion", "[ContentVersion] >= 1"));
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Id).ValueGeneratedNever();

        builder.Property(unit => unit.Type).HasStringEnum(24);
        builder.Property(unit => unit.Code).IsCode(80);
        builder.Property(unit => unit.Status).HasStringEnum(16);
        builder.HasAuditTimestamps();

        builder.HasOne(unit => unit.Vocabulary)
            .WithOne()
            .HasForeignKey<Vocabulary>(vocabulary => vocabulary.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(unit => unit.GrammarPattern)
            .WithOne()
            .HasForeignKey<GrammarPattern>(grammar => grammar.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(unit => unit.Code).HasDatabaseName("UX_KnowledgeUnits_Code").IsUnique();
    }
}
