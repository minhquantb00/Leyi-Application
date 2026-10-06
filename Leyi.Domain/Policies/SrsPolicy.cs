using Leyi.Domain.Knowledge;
using Leyi.Domain.Learning;
using Leyi.Domain.Mastery;

namespace Leyi.Domain.Policies;

/// <summary>
/// SRS configuration (Domain Model §14, Learning Model §22–24.1). Data only: the ReviewScheduler decides
/// outcomes, credit eligibility and the schedule anchor. Values in <see cref="V1"/> are V1 defaults (N1).
/// </summary>
public sealed record SrsPolicy
{
    public static SrsPolicy V1 { get; } = new()
    {
        Version = "srs-v1.0",
        Intervals =
        [
            TimeSpan.FromMinutes(10),
            TimeSpan.FromDays(1),
            TimeSpan.FromDays(3),
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(14),
            TimeSpan.FromDays(30),
            TimeSpan.FromDays(60),
            TimeSpan.FromDays(120),
        ],
        EarlyReviewToleranceRatio = 0.25m,
        PromotionEligibleActivities = new Dictionary<KnowledgeType, IReadOnlySet<ActivityType>>
        {
            [KnowledgeType.Vocabulary] = new HashSet<ActivityType> { ActivityType.MeaningToTypeHanzi },
            [KnowledgeType.Grammar] = new HashSet<ActivityType> { ActivityType.ContextFill },
        },
        DemotionStepsOnQ1 = 1,
        DemotionStepsOnQ0 = 2,
        DemotionFloor = 1,
    };

    public required string Version { get; init; }

    /// <summary>Interval per review level 0–7 (index = level).</summary>
    public required IReadOnlyList<TimeSpan> Intervals { get; init; }

    /// <summary>Early review earns SRS credit when NextReviewAt − now ≤ interval(currentLevel) × ratio.</summary>
    public required decimal EarlyReviewToleranceRatio { get; init; }

    /// <summary>Active-recall activity types that can promote a level ≥ 1 item.</summary>
    public required IReadOnlyDictionary<KnowledgeType, IReadOnlySet<ActivityType>> PromotionEligibleActivities { get; init; }

    public required int DemotionStepsOnQ1 { get; init; }

    public required int DemotionStepsOnQ0 { get; init; }

    public required byte DemotionFloor { get; init; }

    public TimeSpan Level0Interval => Intervals[ReviewLevel.MinValue];

    public TimeSpan IntervalFor(ReviewLevel level) => Intervals[level.Value];

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(Intervals.Count == ReviewLevel.MaxValue + 1, "One interval per review level 0–7 is required.");
        PolicyValidation.Require(Intervals[0] > TimeSpan.Zero, "Intervals must be positive.");
        PolicyValidation.Require(Intervals.Zip(Intervals.Skip(1)).All(pair => pair.First < pair.Second), "Intervals must increase with the level.");
        PolicyValidation.Require(EarlyReviewToleranceRatio is >= 0m and < 1m, "Early review tolerance must be within [0, 1).");
        PolicyValidation.RequireEveryValue(PromotionEligibleActivities.Keys, nameof(PromotionEligibleActivities));
        PolicyValidation.Require(PromotionEligibleActivities.Values.All(types => types.Count > 0 && types.All(type => type.IsAssessment())), "Promotion-eligible types must be assessments.");
        PolicyValidation.Require(DemotionStepsOnQ1 >= 1 && DemotionStepsOnQ0 >= DemotionStepsOnQ1, "Q0 must demote at least as much as Q1.");
        PolicyValidation.Require(DemotionFloor is >= 1 and <= ReviewLevel.MaxValue, "The demotion floor must be a review-cycle level.");
    }
}
