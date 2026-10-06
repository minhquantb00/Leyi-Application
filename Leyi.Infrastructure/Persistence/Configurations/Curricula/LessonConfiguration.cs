using Leyi.Domain.Curricula;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Curricula;

/// <summary>ERD §6.7 Lessons. Code and SortOrder are unique within a level (Domain Model §6.1).</summary>
internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons", table => table.HasCheckConstraint("CK_Lessons_EstimatedMinutes", "[EstimatedMinutes] IS NULL OR [EstimatedMinutes] > 0"));
        builder.HasKey(lesson => lesson.Id);
        builder.Property(lesson => lesson.Id).ValueGeneratedNever();

        builder.Property(lesson => lesson.Code).IsCode(50);
        builder.Property(lesson => lesson.Title).HasMaxLength(200);
        builder.Property(lesson => lesson.Description).HasMaxLength(1000);
        builder.Property(lesson => lesson.Status).HasStringEnum(16);
        builder.HasAuditTimestamps();

        builder.HasOne<CurriculumLevel>()
            .WithMany()
            .HasForeignKey(lesson => lesson.CurriculumLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Join rows are pure children of the lesson (ERD §5: CASCADE).
        builder.HasMany(lesson => lesson.KnowledgeUnits)
            .WithOne()
            .HasForeignKey(link => link.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(lesson => lesson.KnowledgeUnits).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(lesson => new { lesson.CurriculumLevelId, lesson.Code }).HasDatabaseName("UX_Lessons_Level_Code").IsUnique();
        builder.HasIndex(lesson => new { lesson.CurriculumLevelId, lesson.SortOrder }).HasDatabaseName("UX_Lessons_Level_SortOrder").IsUnique();
    }
}
