using Leyi.Domain.Curricula;
using Leyi.Domain.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Curricula;

/// <summary>ERD §6.13 LessonKnowledgeUnits.</summary>
internal sealed class LessonKnowledgeUnitConfiguration : IEntityTypeConfiguration<LessonKnowledgeUnit>
{
    public void Configure(EntityTypeBuilder<LessonKnowledgeUnit> builder)
    {
        builder.ToTable("LessonKnowledgeUnits");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();

        builder.Property(link => link.Role).HasStringEnum(20);
        builder.HasAuditTimestamps(updatedAt: false);

        builder.HasOne<KnowledgeUnit>()
            .WithMany()
            .HasForeignKey(link => link.KnowledgeUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => new { link.LessonId, link.KnowledgeUnitId }).HasDatabaseName("UX_LessonKnowledgeUnits_Lesson_KU").IsUnique();
    }
}
