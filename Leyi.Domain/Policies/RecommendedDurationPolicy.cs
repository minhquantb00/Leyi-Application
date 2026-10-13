using Leyi.Domain.Common;

namespace Leyi.Domain.Policies;

/// <summary>
/// <c>recommendedDurationMinutes</c> on Today (TD-G2; ASE §3.2). A suggestion only: the learner may pick any duration.
/// Bootstrap (newbie) → <see cref="BootstrapMinutes"/>. Otherwise the larger of the duration covering the remaining daily
/// goal and the duration for the review debt. Every result is one of <see cref="AllowedDurationsMinutes"/>. Values in
/// <see cref="V1"/> are V1 defaults (N1); durations and debt thresholds come from <see cref="SessionPolicy"/>.
/// </summary>
public sealed record RecommendedDurationPolicy
{
    public static RecommendedDurationPolicy V1 { get; } = new()
    {
        Version = "recommended-duration-v1.0",
        AllowedDurationsMinutes = SessionPolicy.V1.AllowedDurationsMinutes,
        BootstrapMinutes = 10,
        DebtThresholds = SessionPolicy.V1.ReviewDebt,
        DebtDurations = new ReviewDebtDurations(NoDebt: 3, Low: 5, Medium: 10, High: 15, Critical: 20),
    };

    public required string Version { get; init; }

    /// <summary>Session durations a learner can choose (Today <c>durationOptions</c>).</summary>
    public required IReadOnlyList<int> AllowedDurationsMinutes { get; init; }

    public required int BootstrapMinutes { get; init; }

    /// <summary>Review-debt levels (ASE §23): Low below MediumFrom, then Medium / High / Critical.</summary>
    public required ReviewDebtThresholds DebtThresholds { get; init; }

    public required ReviewDebtDurations DebtDurations { get; init; }

    /// <param name="isBootstrap">Learner in the Bootstrap cold-start phase (<see cref="ColdStartRules.IsBootstrap"/>).</param>
    /// <param name="dailyGoalMinutes">Daily study goal of the active learning goal.</param>
    /// <param name="completedMinutes">Minutes learned today (learner local day).</param>
    /// <param name="reviewDebt">ReviewDebt: reviews with level ≥ 1 due now.</param>
    public int Recommend(bool isBootstrap, int dailyGoalMinutes, int completedMinutes, int reviewDebt)
    {
        Guard.NotNegative(dailyGoalMinutes);
        Guard.NotNegative(completedMinutes);
        Guard.NotNegative(reviewDebt);

        if (isBootstrap)
        {
            return BootstrapMinutes;
        }

        var remainingMinutes = Math.Max(0, dailyGoalMinutes - completedMinutes);
        return Math.Max(GoalDuration(remainingMinutes), DebtDuration(reviewDebt));
    }

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(AllowedDurationsMinutes.Count > 0 && AllowedDurationsMinutes.All(minutes => minutes > 0), "Durations must be positive.");
        PolicyValidation.Require(
            DebtThresholds.MediumFrom > 0 && DebtThresholds.MediumFrom < DebtThresholds.HighFrom && DebtThresholds.HighFrom < DebtThresholds.CriticalFrom,
            "Review-debt thresholds must increase.");
        PolicyValidation.Require(
            DebtDurations.All.Append(BootstrapMinutes).All(AllowedDurationsMinutes.Contains),
            "Recommended durations must be allowed session durations.");
    }

    /// <summary>Smallest allowed duration covering the remaining goal; the longest one when none does.</summary>
    private int GoalDuration(int remainingMinutes)
    {
        var ordered = AllowedDurationsMinutes.Order().ToList();
        return ordered.FirstOrDefault(minutes => minutes >= remainingMinutes, ordered[^1]);
    }

    private int DebtDuration(int reviewDebt) => reviewDebt switch
    {
        0 => DebtDurations.NoDebt,
        _ when reviewDebt < DebtThresholds.MediumFrom => DebtDurations.Low,
        _ when reviewDebt < DebtThresholds.HighFrom => DebtDurations.Medium,
        _ when reviewDebt < DebtThresholds.CriticalFrom => DebtDurations.High,
        _ => DebtDurations.Critical,
    };
}

/// <summary>Recommended minutes per review-debt level; <see cref="NoDebt"/> when nothing is due.</summary>
public sealed record ReviewDebtDurations(int NoDebt, int Low, int Medium, int High, int Critical)
{
    public IEnumerable<int> All => [NoDebt, Low, Medium, High, Critical];
}
