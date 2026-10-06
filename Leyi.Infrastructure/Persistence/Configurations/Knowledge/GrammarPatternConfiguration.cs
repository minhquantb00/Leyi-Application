using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Knowledge;

/// <summary>ERD §6.10 GrammarPatterns.</summary>
internal sealed class GrammarPatternConfiguration : IEntityTypeConfiguration<GrammarPattern>
{
    public void Configure(EntityTypeBuilder<GrammarPattern> builder)
    {
        builder.ToTable("GrammarPatterns", table => table.HasCheckConstraint("CK_GrammarPatterns_HskLevel", "[HskLevel] IS NULL OR [HskLevel] BETWEEN 1 AND 6"));
        builder.HasKey(grammar => grammar.KnowledgeUnitId);
        builder.Property(grammar => grammar.KnowledgeUnitId).ValueGeneratedNever();

        builder.Property(grammar => grammar.Pattern).HasMaxLength(300);
        builder.HasAuditTimestamps();
    }
}
