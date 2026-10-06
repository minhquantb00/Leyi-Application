using Leyi.Domain.Curricula;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Curricula;

/// <summary>ERD §6.6 CurriculumLevels. Code and SortOrder are unique within a curriculum (Domain Model §6.1).</summary>
internal sealed class CurriculumLevelConfiguration : IEntityTypeConfiguration<CurriculumLevel>
{
    public void Configure(EntityTypeBuilder<CurriculumLevel> builder)
    {
        builder.ToTable("CurriculumLevels");
        builder.HasKey(level => level.Id);
        builder.Property(level => level.Id).ValueGeneratedNever();

        builder.Property(level => level.Code).IsCode(30);
        builder.Property(level => level.Name).HasMaxLength(100);
        builder.HasAuditTimestamps();

        builder.HasOne<Curriculum>()
            .WithMany()
            .HasForeignKey(level => level.CurriculumId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(level => new { level.CurriculumId, level.Code }).HasDatabaseName("UX_CurriculumLevels_Curriculum_Code").IsUnique();
        builder.HasIndex(level => new { level.CurriculumId, level.SortOrder }).HasDatabaseName("UX_CurriculumLevels_Curriculum_SortOrder").IsUnique();
    }
}
