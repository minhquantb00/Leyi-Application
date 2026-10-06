using Leyi.Domain.Curricula;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Curricula;

/// <summary>ERD §6.5 Curriculums.</summary>
internal sealed class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
{
    public void Configure(EntityTypeBuilder<Curriculum> builder)
    {
        builder.ToTable("Curriculums");
        builder.HasKey(curriculum => curriculum.Id);
        builder.Property(curriculum => curriculum.Id).ValueGeneratedNever();

        builder.Property(curriculum => curriculum.Code).IsCode(50);
        builder.Property(curriculum => curriculum.Name).HasMaxLength(200);
        builder.Property(curriculum => curriculum.Version).IsCode(30);
        builder.Property(curriculum => curriculum.Standard).IsCode(30);
        builder.Property(curriculum => curriculum.Status).HasStringEnum(16);
        builder.HasAuditTimestamps();

        builder.HasIndex(curriculum => curriculum.Code).HasDatabaseName("UX_Curriculums_Code").IsUnique();
    }
}
