using Leyi.Domain.Common;

namespace Leyi.Domain.Progress;

/// <summary>
/// Curriculum progress of one lesson for one user (ERD §6.14). Separate from knowledge mastery:
/// completing a lesson never changes mastery (Learning Model §47–48). Start and Complete are idempotent.
/// </summary>
public sealed class UserLessonProgress : Entity
{
    private UserLessonProgress()
    {
    }

    private UserLessonProgress(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public Guid LessonId { get; private set; }

    public LessonProgressStatus Status { get; private set; }

    public decimal ProgressPercent { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public static UserLessonProgress Create(Guid userId, Guid lessonId) => new(NewId())
    {
        UserId = Guard.NotEmpty(userId),
        LessonId = Guard.NotEmpty(lessonId),
        Status = LessonProgressStatus.NotStarted,
    };

    /// <summary>NOT_STARTED → IN_PROGRESS. No change when already started or completed (no reset).</summary>
    public void Start(DateTime nowUtc)
    {
        if (Status != LessonProgressStatus.NotStarted)
        {
            return;
        }

        Status = LessonProgressStatus.InProgress;
        StartedAt = Guard.Utc(nowUtc);
    }

    public void UpdateProgress(decimal progressPercent)
    {
        if (Status == LessonProgressStatus.Completed)
        {
            return;
        }

        ProgressPercent = Guard.InRange(progressPercent, 0m, 100m);
    }

    /// <summary>Marks first-pass completion. <see cref="CompletedAt"/> is set only on the first completion.</summary>
    public void Complete(DateTime nowUtc)
    {
        if (Status == LessonProgressStatus.Completed)
        {
            return;
        }

        Guard.Utc(nowUtc);
        StartedAt ??= nowUtc;
        Status = LessonProgressStatus.Completed;
        ProgressPercent = 100m;
        CompletedAt = nowUtc;
    }
}
