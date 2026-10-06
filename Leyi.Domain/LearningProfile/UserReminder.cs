using Leyi.Domain.Common;

namespace Leyi.Domain.LearningProfile;

/// <summary>Study reminder (ERD §6.4). <see cref="ReminderTime"/> is local time in <see cref="Timezone"/>.</summary>
public sealed class UserReminder : Entity
{
    /// <summary>Every day of the week (Mon–Sun bit mask).</summary>
    public const byte AllDays = 127;

    private UserReminder()
    {
    }

    private UserReminder(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public bool Enabled { get; private set; }

    public TimeOnly? ReminderTime { get; private set; }

    /// <summary>IANA timezone id.</summary>
    public string Timezone { get; private set; } = string.Empty;

    public byte DaysMask { get; private set; }

    public static UserReminder Create(Guid userId, bool enabled, TimeOnly? reminderTime, string timezone, byte daysMask = AllDays)
    {
        var reminder = new UserReminder(NewId()) { UserId = Guard.NotEmpty(userId) };
        reminder.Update(enabled, reminderTime, timezone, daysMask);
        return reminder;
    }

    public void Update(bool enabled, TimeOnly? reminderTime, string timezone, byte daysMask)
    {
        if (daysMask > AllDays)
        {
            throw new ArgumentOutOfRangeException(nameof(daysMask), daysMask, "Days mask only has seven bits.");
        }

        if (enabled && reminderTime is null)
        {
            throw new ArgumentException("An enabled reminder needs a time.", nameof(reminderTime));
        }

        Enabled = enabled;
        ReminderTime = reminderTime;
        Timezone = Guard.NotBlank(timezone);
        DaysMask = daysMask;
    }
}
