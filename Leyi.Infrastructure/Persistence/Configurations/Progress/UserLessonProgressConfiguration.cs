using Leyi.Domain.Curricula;
using Leyi.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.Progress;

/// <summary>ERD §6.14 UserLessonProgress (curriculum progress; separate from mastery).</summary>
internal sealed class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
{
    public void Configure(EntityTypeBuilder<UserLessonProgress> builder)
    {
        builder.ToTable("UserLessonProgress", table => table.HasCheckConstraint("CK_UserLessonProgress_ProgressPercent", "[ProgressPercent] BETWEEN 0 AND 100"));
        builder.HasKey(progress => progress.Id);
        builder.Property(progress => progress.Id).ValueGeneratedNever();

        builder.Property(progress => progress.Status).HasStringEnum(20);
        builder.Property(progress => progress.ProgressPercent).HasPrecision(5, 2);
        builder.HasAuditTimestamps();

        builder.HasUser(progress => progress.UserId, DeleteBehavior.Restrict);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(progress => progress.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(progress => new { progress.UserId, progress.LessonId }).HasDatabaseName("UX_UserLessonProgress_User_Lesson").IsUnique();
    }
}
