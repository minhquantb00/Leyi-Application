using Leyi.Domain.Learning;
using Leyi.Domain.LearningProfile;
using Leyi.Domain.Mastery;

namespace Leyi.Domain.Sessions.Scoring;

/// <summary>
/// What <see cref="SessionPriorityCalculator.Score"/> needs for one assessment candidate. <paramref name="ReviewStatus"/>
/// is the typed classification from <see cref="SessionPriorityCalculator.ClassifyReview"/> (the single classifier); the
/// calculator derives the review reason from it (DueNow / DueToday → DUE_REVIEW, Overdue → OVERDUE), never from the
/// urgency value. <see cref="ReviewDueStatus.None"/> goes with a review urgency of 0 and any other status with a positive one.
/// </summary>
/// <param name="Components">The five priority signals.</param>
/// <param name="ActivityType">Assessment activity type; its evidence weight is the EvidenceValue and its primary dimension selects the GoalModifier.</param>
/// <param name="PrimaryGoal">The learner's active goal.</param>
/// <param name="ReviewLevel">Current review level; null when the KU has no mastery yet (NEW).</param>
/// <param name="Confidence">Mastery confidence; null when the KU has no mastery yet.</param>
/// <param name="ReviewStatus">Review classification of the KU at request time; labels the ReviewUrgency contribution.</param>
public sealed record CandidateScoringInput(
    PriorityComponents Components,
    ActivityType ActivityType,
    PrimaryGoal PrimaryGoal,
    ReviewLevel? ReviewLevel,
    ConfidenceLevel? Confidence,
    ReviewDueStatus ReviewStatus);

/// <summary>One additive part of the FinalScore, labelled with the reason it explains (AS-G15). Full precision.</summary>
public sealed record ScoreContribution(SelectionReason Reason, decimal Value);

/// <summary>
/// Score of one candidate (ASE §11, §19): <c>FinalScore = PriorityScore × EvidenceValue × GoalModifier × ConfidenceModifier</c>,
/// not clamped (modifiers may lift it above 100). <see cref="Contributions"/> add up to <see cref="FinalScore"/>.
/// </summary>
public sealed record CandidateScore(
    decimal PriorityScore,
    decimal EvidenceValue,
    decimal GoalModifier,
    decimal ConfidenceModifier,
    decimal FinalScore,
    IReadOnlyList<ScoreContribution> Contributions)
{
    /// <summary>Scale of <c>SessionActivityReasons.ScoreContribution</c> (decimal(6,2)).</summary>
    public const int ContributionDecimals = 2;

    /// <summary>
    /// The reasons to persist: each contribution rounded once, at this point only, to <see cref="ContributionDecimals"/>
    /// (away from zero, the project's score rounding).
    /// </summary>
    public IReadOnlyList<ActivitySelectionReason> ToSelectionReasons() =>
        Contributions
            .Select(contribution => new ActivitySelectionReason(
                contribution.Reason,
                decimal.Round(contribution.Value, ContributionDecimals, MidpointRounding.AwayFromZero)))
            .ToList();
}
