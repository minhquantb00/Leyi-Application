namespace Leyi.Domain.Sessions.Scoring;

/// <summary>
/// Review timing of a knowledge unit at request time (LM §22.1, ASE §7, §13). Level 0 is never a review (it is the
/// Reinforcement pool), so it has no due status.
/// </summary>
public enum ReviewDueStatus
{
    /// <summary>Level 0, or level ≥ 1 not due before the end of the learner's local day: no review urgency.</summary>
    None,

    /// <summary>Level ≥ 1, now &lt; NextReviewAt &lt; end of the learner's local day (secondary candidate).</summary>
    DueToday,

    /// <summary>Level ≥ 1, NextReviewAt ≤ now with OverdueRatio below the overdue threshold (Pool A).</summary>
    DueNow,

    /// <summary>Level ≥ 1, OverdueRatio at or above the overdue threshold (Pool B).</summary>
    Overdue,
}

/// <param name="Status">Due classification.</param>
/// <param name="OverdueRatio"><c>(now − NextReviewAt) / interval(ReviewLevel)</c> for DueNow/Overdue; 0 otherwise.</param>
public sealed record ReviewTiming(ReviewDueStatus Status, decimal OverdueRatio)
{
    public static ReviewTiming NotDue { get; } = new(ReviewDueStatus.None, 0m);
}
