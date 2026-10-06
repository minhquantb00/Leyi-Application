namespace Leyi.Domain.Progress;

/// <summary>Curriculum progress status (ERD §6.14). Never implies knowledge mastery.</summary>
public enum LessonProgressStatus
{
    NotStarted,
    InProgress,
    Completed,
}
