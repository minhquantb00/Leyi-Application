namespace Leyi.Domain.Sessions;

/// <summary>
/// Why an activity was selected (ERD §6.20, Adaptive Session Engine §64). CORRECTION marks a re-ask after an error;
/// it is a reason, never an activity type.
/// </summary>
public enum SelectionReason
{
    DueReview,
    Overdue,
    WeakDimension,
    RecentError,
    Curriculum,
    Reinforcement,
    GoalRelevance,
    LowConfidence,
    Correction,
}
