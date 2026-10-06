using Leyi.Domain.LearningProfile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leyi.Infrastructure.Persistence.Configurations.LearningProfile;

/// <summary>ERD §6.4 UserReminders (settings, not learning history: CASCADE from Users).</summary>
internal sealed class UserReminderConfiguration : IEntityTypeConfiguration<UserReminder>
{
    public void Configure(EntityTypeBuilder<UserReminder> builder)
    {
        builder.ToTable("UserReminders", table => table.HasCheckConstraint("CK_UserReminders_DaysMask", "[DaysMask] BETWEEN 0 AND 127"));
        builder.HasKey(reminder => reminder.Id);
        builder.Property(reminder => reminder.Id).ValueGeneratedNever();

        builder.Property(reminder => reminder.ReminderTime).HasColumnType("time(0)");
        builder.Property(reminder => reminder.Timezone).IsCode(64);
        builder.HasAuditTimestamps();

        builder.HasUser(reminder => reminder.UserId, DeleteBehavior.Cascade);
    }
}
