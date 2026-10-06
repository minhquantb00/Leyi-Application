using Leyi.Domain.Learning;

namespace Leyi.Domain.Policies;

/// <summary>
/// Evidence configuration (Domain Model §14, Learning Model §12, §28–30, §33.1, §43, ASE §34). Data only.
/// Response-time baselines per activity type (Q3 "slow" cap) are not specified yet and are intentionally absent.
/// Values in <see cref="V1"/> are V1 defaults (N1).
/// </summary>
public sealed record EvidencePolicy
{
    public static EvidencePolicy V1 { get; } = new()
    {
        Version = "evidence-v1.0",
        QualityValues = new Dictionary<EvidenceQuality, decimal>
        {
            [EvidenceQuality.Q4] = 1.00m,
            [EvidenceQuality.Q3] = 0.85m,
            [EvidenceQuality.Q2] = 0.60m,
            [EvidenceQuality.Q1] = 0.35m,
            [EvidenceQuality.Q0] = 0.00m,
        },
        AssessmentRules = new Dictionary<ActivityType, AssessmentRule>
        {
            [ActivityType.HanziToMeaning] = new(MasteryDimension.Recognition, 0.7m, 1),
            [ActivityType.MeaningToHanzi] = new(MasteryDimension.Recall, 0.6m, 1),
            [ActivityType.MeaningToTypeHanzi] = new(MasteryDimension.Recall, 1.0m, 2),
            [ActivityType.ListeningToHanzi] = new(MasteryDimension.Listening, 0.9m, 1),
            [ActivityType.ListeningToMeaning] = new(MasteryDimension.Listening, 0.8m, 1),
            [ActivityType.ContextFill] = new(MasteryDimension.Usage, 0.9m, 1),
        },
        AudioReplayThreshold = 2,
        SameSessionFactors = [1.0m, 0.7m, 0.4m],
        RecentErrorWindow = TimeSpan.FromDays(14),
    };

    public required string Version { get; init; }

    /// <summary>Target value of each quality (Target = value × 100).</summary>
    public required IReadOnlyDictionary<EvidenceQuality, decimal> QualityValues { get; init; }

    /// <summary>Primary dimension, evidence weight and max attempts per assessment activity type (primary dimension only in V1).</summary>
    public required IReadOnlyDictionary<ActivityType, AssessmentRule> AssessmentRules { get; init; }

    /// <summary>More replays than this caps quality at Q3.</summary>
    public required int AudioReplayThreshold { get; init; }

    /// <summary>Same-session discount for gains by evidence ordinal of the same knowledge unit; the last value applies beyond.</summary>
    public required IReadOnlyList<decimal> SameSessionFactors { get; init; }

    /// <summary>RecentErrorCount window: Q0 evidence with CreatedAt ≥ now − window.</summary>
    public required TimeSpan RecentErrorWindow { get; init; }

    public AssessmentRule RuleFor(ActivityType activityType) =>
        AssessmentRules.TryGetValue(activityType, out var rule)
            ? rule
            : throw new InvalidOperationException($"{activityType} is not an assessment activity in evidence policy {Version}.");

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.RequireEveryValue(QualityValues.Keys, nameof(QualityValues));
        PolicyValidation.Require(QualityValues.Values.All(PolicyValidation.IsFraction), "Quality values must be within 0–1.");
        PolicyValidation.Require(
            Enum.GetValues<EvidenceQuality>().Zip(Enum.GetValues<EvidenceQuality>().Skip(1)).All(pair => QualityValues[pair.First] < QualityValues[pair.Second]),
            "Quality values must increase from Q0 to Q4.");

        var assessmentTypes = Enum.GetValues<ActivityType>().Where(type => type.IsAssessment()).ToHashSet();
        PolicyValidation.Require(AssessmentRules.Keys.ToHashSet().SetEquals(assessmentTypes), "Assessment rules must cover exactly the assessment activity types.");
        PolicyValidation.Require(AssessmentRules.Values.All(rule => rule.EvidenceWeight is > 0m and <= 1m && rule.MaxAttempts >= 1), "Evidence weights must be in (0, 1] and attempts ≥ 1.");
        PolicyValidation.Require(AudioReplayThreshold >= 0, "Replay threshold must not be negative.");
        PolicyValidation.Require(SameSessionFactors.Count > 0 && SameSessionFactors[0] == 1m, "The first same-session evidence is not discounted.");
        PolicyValidation.Require(SameSessionFactors.All(factor => factor is > 0m and <= 1m), "Same-session factors must be in (0, 1].");
        PolicyValidation.Require(SameSessionFactors.Zip(SameSessionFactors.Skip(1)).All(pair => pair.Second <= pair.First), "Same-session factors must not increase.");
        PolicyValidation.Require(RecentErrorWindow > TimeSpan.Zero, "Recent error window must be positive.");
    }
}

/// <summary>Assessment rule of one activity type.</summary>
public sealed record AssessmentRule(MasteryDimension PrimaryDimension, decimal EvidenceWeight, int MaxAttempts);
