namespace Leyi.Domain.Sessions;

/// <summary>
/// Learning session type V1 (ERD §6.18). There is no DAILY_RECOMMENDED type: the daily recommendation is only a
/// suggested duration for an adaptive quick session. PLACEMENT is reserved for the V1.1 placement test.
/// </summary>
public enum SessionType
{
    AdaptiveQuick,
    Lesson,
    Practice,
    Placement,
}
