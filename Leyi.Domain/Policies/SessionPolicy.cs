using Leyi.Domain.Common;
using Leyi.Domain.Learning;
using Leyi.Domain.Mastery;
using Leyi.Domain.Sessions;

namespace Leyi.Domain.Policies;

/// <summary>
/// Adaptive session configuration (Domain Model §14, Adaptive Session Engine §5–6, §11–31, §45–54, §73).
/// Data only: CandidateBuilder / PriorityCalculator / SessionPlanner consume it; the session aggregate uses
/// <see cref="TimingRules"/>. Values in <see cref="V1"/> are V1 defaults (N1).
/// </summary>
public sealed record SessionPolicy
{
    public static SessionPolicy V1 { get; } = new()
    {
        Version = "adaptive-v1.0",
        AllowedDurationsMinutes = [3, 5, 7, 10, 15, 20],
        EffectiveBudgetRatio = 0.90m,
        ActivityDurations = new Dictionary<ActivityType, TimeSpan>
        {
            [ActivityType.HanziToMeaning] = TimeSpan.FromSeconds(15),
            [ActivityType.MeaningToHanzi] = TimeSpan.FromSeconds(18),
            [ActivityType.MeaningToTypeHanzi] = TimeSpan.FromSeconds(25),
            [ActivityType.ListeningToHanzi] = TimeSpan.FromSeconds(20),
            [ActivityType.ListeningToMeaning] = TimeSpan.FromSeconds(20),
            [ActivityType.ContextFill] = TimeSpan.FromSeconds(25),
            [ActivityType.NewVocabularyExposure] = TimeSpan.FromSeconds(35),
        },
        PriorityWeights = new PriorityWeights(ReviewUrgency: 0.35m, DimensionWeakness: 0.30m, RecentError: 0.20m, CurriculumRelevance: 0.10m, ReinforcementNeed: 0.05m),
        ReviewUrgency = new ReviewUrgencyScores(DueToday: 20m, DueNow: 50m, OneIntervalOverdue: 80m, TwoIntervalsOverdue: 100m),
        OverdueThresholdRatio = 1.0m,
        WeaknessScorePerGapPoint = 2.5m,
        UnmeasuredDimensionWeakness = 50m,
        RecentErrorScores = [0m, 40m, 70m, 100m],
        CurriculumRelevance = new CurriculumRelevanceScores(CurrentLesson: 100m, NextLesson: 70m, PreviousLesson: 40m, OtherLesson: 10m),
        Reinforcement = new ReinforcementScores(WithinTenMinutes: 80m, WithinDay: 50m, Older: 10m),
        ReinforcementWindows = new ReinforcementWindows(Recent: TimeSpan.FromMinutes(10), Day: TimeSpan.FromHours(24)),
        ConfidenceModifiers = new Dictionary<ConfidenceLevel, decimal>
        {
            [ConfidenceLevel.Low] = 1.20m,
            [ConfidenceLevel.Medium] = 1.10m,
            [ConfidenceLevel.High] = 1.00m,
        },
        ReviewDebt = new ReviewDebtThresholds(MediumFrom: 10, HighFrom: 31, CriticalFrom: 61),
        AdaptiveNewShare = new DebtShares(Low: 0.40m, Medium: 0.25m, High: 0.10m, Critical: 0m),
        DebtNewShareCap = new DebtShares(Low: 1.00m, Medium: 0.25m, High: 0.10m, Critical: 0m),
        ShortSessionMaxNewShare = new ShortSessionNewShare(MaxDurationMinutes: 3, MaxNewShare: 0.20m),
        ColdStart = new ColdStartRules(
            MeaningfulSessionMinEvaluatedActivities: 3,
            BootstrapBeforeSessions: 3,
            BootstrapNewShare: 0.80m,
            AdaptiveAfterSessions: 10,
            RampNewShare: 0.60m,
            EarlyAdaptiveMinSessions: 5,
            EarlyAdaptiveMinEvidence: 60),
        Diversity = new DiversityRules(MaxSameTypeInARow: 3, MinTypesFromFiveMinutes: 2, MinTypesFromTenMinutes: 3, MaxSameKnowledgeInARow: 2),
        SkillImbalanceThreshold = 15m,
        WeakSkillShareMin = 0.20m,
        WeakSkillShareMax = 0.40m,
        WeakSkillTargetShares = new PhaseShares(Bootstrap: 0.20m, Ramp: 0.30m, Adaptive: 0.40m),
        IdleCap = TimeSpan.FromSeconds(120),
        ContinueThreshold = TimeSpan.FromMinutes(30),
        ExpiryThreshold = TimeSpan.FromHours(24),
    };

    public required string Version { get; init; }

    public required IReadOnlyList<int> AllowedDurationsMinutes { get; init; }

    public required decimal EffectiveBudgetRatio { get; init; }

    public required IReadOnlyDictionary<ActivityType, TimeSpan> ActivityDurations { get; init; }

    public required PriorityWeights PriorityWeights { get; init; }

    public required ReviewUrgencyScores ReviewUrgency { get; init; }

    /// <summary>OverdueRatio at which a due item joins the Overdue pool.</summary>
    public required decimal OverdueThresholdRatio { get; init; }

    /// <summary>DimensionWeakness = min(100, gap × factor): gap 10 → 25, 20 → 50, 30 → 75, ≥ 40 → 100.</summary>
    public required decimal WeaknessScorePerGapPoint { get; init; }

    public required decimal UnmeasuredDimensionWeakness { get; init; }

    /// <summary>RecentError score by error count (index); the last value applies to higher counts.</summary>
    public required IReadOnlyList<decimal> RecentErrorScores { get; init; }

    public required CurriculumRelevanceScores CurriculumRelevance { get; init; }

    public required ReinforcementScores Reinforcement { get; init; }

    /// <summary>Elapsed-time windows of the ReinforcementNeed signal (ASE §18): &lt; Recent, &lt; Day, otherwise older.</summary>
    public required ReinforcementWindows ReinforcementWindows { get; init; }

    /// <summary>FinalScore modifier by confidence, applied only to items with ReviewLevel ≥ 1.</summary>
    public required IReadOnlyDictionary<ConfidenceLevel, decimal> ConfidenceModifiers { get; init; }

    public required ReviewDebtThresholds ReviewDebt { get; init; }

    /// <summary>New-content share in the adaptive phase by review-debt level.</summary>
    public required DebtShares AdaptiveNewShare { get; init; }

    /// <summary>Upper bound on new content in every phase by review-debt level (NewShare = min(phase share, cap)).</summary>
    public required DebtShares DebtNewShareCap { get; init; }

    public required ShortSessionNewShare ShortSessionMaxNewShare { get; init; }

    public required ColdStartRules ColdStart { get; init; }

    public required DiversityRules Diversity { get; init; }

    /// <summary>A dimension this many points below the average of the others is a weak skill.</summary>
    public required decimal SkillImbalanceThreshold { get; init; }

    public required decimal WeakSkillShareMin { get; init; }

    public required decimal WeakSkillShareMax { get; init; }

    /// <summary>Soft weak-skill target share of the effective budget (seconds) per cold-start phase (AS-G5).</summary>
    public required PhaseShares WeakSkillTargetShares { get; init; }

    public required TimeSpan IdleCap { get; init; }

    public required TimeSpan ContinueThreshold { get; init; }

    public required TimeSpan ExpiryThreshold { get; init; }

    public SessionTimingRules TimingRules => new(IdleCap, ContinueThreshold, ExpiryThreshold);

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(AllowedDurationsMinutes.Count > 0 && AllowedDurationsMinutes.All(minutes => minutes > 0), "Durations must be positive.");
        PolicyValidation.Require(AllowedDurationsMinutes.Distinct().Count() == AllowedDurationsMinutes.Count, "Durations must be distinct.");
        PolicyValidation.Require(EffectiveBudgetRatio is > 0m and <= 1m, "Effective budget ratio must be in (0, 1].");
        PolicyValidation.RequireEveryValue(ActivityDurations.Keys, nameof(ActivityDurations));
        PolicyValidation.Require(ActivityDurations.Values.All(duration => duration > TimeSpan.Zero), "Activity durations must be positive.");
        PolicyValidation.RequireSumIsOne(PriorityWeights.All, nameof(PriorityWeights));
        PolicyValidation.Require(PriorityWeights.All.All(PolicyValidation.IsFraction), "Priority weights must be within 0–1.");
        PolicyValidation.Require(ReviewUrgency.All.All(PolicyValidation.IsScore), "Review urgency values must be scores.");
        PolicyValidation.Require(
            ReviewUrgency.DueToday < ReviewUrgency.DueNow && ReviewUrgency.DueNow < ReviewUrgency.OneIntervalOverdue && ReviewUrgency.OneIntervalOverdue <= ReviewUrgency.TwoIntervalsOverdue,
            "Review urgency must increase from DueToday to overdue.");
        PolicyValidation.Require(OverdueThresholdRatio > 0m, "Overdue threshold must be positive.");
        PolicyValidation.Require(WeaknessScorePerGapPoint > 0m && PolicyValidation.IsScore(UnmeasuredDimensionWeakness), "Weakness scoring must be positive.");
        PolicyValidation.Require(RecentErrorScores.Count > 0 && RecentErrorScores[0] == 0m && RecentErrorScores.All(PolicyValidation.IsScore), "Recent error scores must start at 0.");
        PolicyValidation.Require(CurriculumRelevance.All.All(PolicyValidation.IsScore) && Reinforcement.All.All(PolicyValidation.IsScore), "Signal scores must be scores.");

        // Every scored assessment — a fallback candidate with no other signal included — must carry at least one truthful
        // SelectionReason (SessionActivity invariant): CURRICULUM, which needs a positive weight and positive position scores.
        PolicyValidation.Require(PriorityWeights.CurriculumRelevance > 0m && CurriculumRelevance.All.All(score => score > 0m), "Curriculum relevance must be positive for every position.");
        PolicyValidation.RequireEveryValue(ConfidenceModifiers.Keys, nameof(ConfidenceModifiers));
        PolicyValidation.Require(ConfidenceModifiers.Values.All(modifier => modifier >= 1m), "Confidence modifiers never reduce priority.");
        PolicyValidation.Require(0 < ReviewDebt.MediumFrom && ReviewDebt.MediumFrom < ReviewDebt.HighFrom && ReviewDebt.HighFrom < ReviewDebt.CriticalFrom, "Review debt thresholds must increase.");
        PolicyValidation.Require(AdaptiveNewShare.All.All(PolicyValidation.IsFraction) && DebtNewShareCap.All.All(PolicyValidation.IsFraction), "New shares must be fractions.");
        PolicyValidation.Require(AdaptiveNewShare.Critical == 0m && DebtNewShareCap.Critical == 0m, "Critical review debt allows no new content.");
        PolicyValidation.Require(PolicyValidation.IsFraction(ShortSessionMaxNewShare.MaxNewShare), "Short-session new share must be a fraction.");
        PolicyValidation.Require(
            ColdStart.BootstrapBeforeSessions > 0
                && ColdStart.BootstrapBeforeSessions < ColdStart.AdaptiveAfterSessions
                && ColdStart.EarlyAdaptiveMinSessions <= ColdStart.AdaptiveAfterSessions
                && ColdStart.MeaningfulSessionMinEvaluatedActivities > 0
                && ColdStart.EarlyAdaptiveMinEvidence > 0
                && PolicyValidation.IsFraction(ColdStart.BootstrapNewShare)
                && PolicyValidation.IsFraction(ColdStart.RampNewShare),
            "Cold-start phases are inconsistent.");
        PolicyValidation.Require(Diversity.MaxSameTypeInARow > 0 && Diversity.MaxSameKnowledgeInARow > 0, "Diversity limits must be positive.");
        PolicyValidation.Require(SkillImbalanceThreshold > 0m && WeakSkillShareMin <= WeakSkillShareMax && PolicyValidation.IsFraction(WeakSkillShareMax), "Weak skill allocation is inconsistent.");
        PolicyValidation.Require(
            WeakSkillTargetShares.All.All(share => share >= WeakSkillShareMin && share <= WeakSkillShareMax),
            "Weak skill target shares must be within the weak skill share range.");
        PolicyValidation.Require(
            ReinforcementWindows.Recent > TimeSpan.Zero && ReinforcementWindows.Recent < ReinforcementWindows.Day,
            "Reinforcement windows must be positive and increasing.");
        _ = TimingRules;
    }

    /// <summary>ReviewDebt level (ASE §23): Low below MediumFrom, Medium below HighFrom, High below CriticalFrom.</summary>
    public ReviewDebtLevel DebtLevelOf(int reviewDebt) => Guard.NotNegative(reviewDebt) switch
    {
        var debt when debt < ReviewDebt.MediumFrom => ReviewDebtLevel.Low,
        var debt when debt < ReviewDebt.HighFrom => ReviewDebtLevel.Medium,
        var debt when debt < ReviewDebt.CriticalFrom => ReviewDebtLevel.High,
        _ => ReviewDebtLevel.Critical,
    };

    /// <summary>
    /// Ordinary new-content cap as a share of the effective budget (ASE §24–25, AS-G5): <c>min(PhaseNewShare, DebtCap)</c>,
    /// further capped for short sessions. A cap, not a quota. The X1 exception (no non-new candidate) belongs to the
    /// planner and never applies to Critical debt (cap 0).
    /// </summary>
    public decimal NewShareCap(ColdStartPhase phase, ReviewDebtLevel debtLevel, int durationMinutes)
    {
        Guard.Positive(durationMinutes);
        var phaseShare = Guard.Defined(phase) switch
        {
            ColdStartPhase.Bootstrap => ColdStart.BootstrapNewShare,
            ColdStartPhase.Ramp => ColdStart.RampNewShare,
            _ => AdaptiveNewShare.For(debtLevel),
        };

        var cap = Math.Min(phaseShare, DebtNewShareCap.For(debtLevel));
        return durationMinutes <= ShortSessionMaxNewShare.MaxDurationMinutes ? Math.Min(cap, ShortSessionMaxNewShare.MaxNewShare) : cap;
    }

    /// <summary>Soft weak-skill target share for the phase (AS-G5); applies only when <see cref="WeakSkills"/> finds a weak dimension.</summary>
    public decimal WeakSkillTargetShare(ColdStartPhase phase) => WeakSkillTargetShares.For(phase);

    /// <summary>
    /// Weak skills for the planner (ASE §54, AS-G5): every dimension whose average is at least
    /// <see cref="SkillImbalanceThreshold"/> below the plain mean of the averages of the <em>other</em> eligible
    /// dimensions. Several dimensions may qualify; none is picked over another. Fewer than two eligible dimensions →
    /// none. Eligibility (samples with enough evidence) is the caller's; this is not Today's WeakestDimensionPolicy.
    /// </summary>
    public IReadOnlyList<MasteryDimension> WeakSkills(IReadOnlyCollection<DimensionAverage> eligibleAverages)
    {
        ArgumentNullException.ThrowIfNull(eligibleAverages);
        if (eligibleAverages.Select(average => average.Dimension).Distinct().Count() != eligibleAverages.Count)
        {
            throw new ArgumentException("Each dimension may appear only once.", nameof(eligibleAverages));
        }

        if (eligibleAverages.Count < 2)
        {
            return [];
        }

        return eligibleAverages
            .Where(candidate =>
            {
                var others = eligibleAverages.Where(average => average.Dimension != candidate.Dimension).ToList();
                var othersMean = others.Sum(average => average.AverageScore) / others.Count;
                return othersMean - candidate.AverageScore >= SkillImbalanceThreshold;
            })
            .Select(average => average.Dimension)
            .Order()
            .ToList();
    }
}

public sealed record PriorityWeights(decimal ReviewUrgency, decimal DimensionWeakness, decimal RecentError, decimal CurriculumRelevance, decimal ReinforcementNeed)
{
    public IEnumerable<decimal> All => [ReviewUrgency, DimensionWeakness, RecentError, CurriculumRelevance, ReinforcementNeed];
}

/// <summary>ReviewUrgency signal; values between the overdue points are interpolated by OverdueRatio.</summary>
public sealed record ReviewUrgencyScores(decimal DueToday, decimal DueNow, decimal OneIntervalOverdue, decimal TwoIntervalsOverdue)
{
    public IEnumerable<decimal> All => [DueToday, DueNow, OneIntervalOverdue, TwoIntervalsOverdue];
}

public sealed record CurriculumRelevanceScores(decimal CurrentLesson, decimal NextLesson, decimal PreviousLesson, decimal OtherLesson)
{
    public IEnumerable<decimal> All => [CurrentLesson, NextLesson, PreviousLesson, OtherLesson];
}

public sealed record ReinforcementScores(decimal WithinTenMinutes, decimal WithinDay, decimal Older)
{
    public IEnumerable<decimal> All => [WithinTenMinutes, WithinDay, Older];
}

/// <summary>ReviewDebt levels: Low &lt; MediumFrom ≤ Medium &lt; HighFrom ≤ High &lt; CriticalFrom ≤ Critical.</summary>
public sealed record ReviewDebtThresholds(int MediumFrom, int HighFrom, int CriticalFrom);

public sealed record ReinforcementWindows(TimeSpan Recent, TimeSpan Day);

public sealed record DebtShares(decimal Low, decimal Medium, decimal High, decimal Critical)
{
    public IEnumerable<decimal> All => [Low, Medium, High, Critical];

    public decimal For(ReviewDebtLevel level) => Guard.Defined(level) switch
    {
        ReviewDebtLevel.Low => Low,
        ReviewDebtLevel.Medium => Medium,
        ReviewDebtLevel.High => High,
        _ => Critical,
    };
}

public sealed record PhaseShares(decimal Bootstrap, decimal Ramp, decimal Adaptive)
{
    public IEnumerable<decimal> All => [Bootstrap, Ramp, Adaptive];

    public decimal For(ColdStartPhase phase) => Guard.Defined(phase) switch
    {
        ColdStartPhase.Bootstrap => Bootstrap,
        ColdStartPhase.Ramp => Ramp,
        _ => Adaptive,
    };
}

public sealed record ShortSessionNewShare(int MaxDurationMinutes, decimal MaxNewShare);

/// <summary>
/// Cold-start phases by meaningful sessions (ended with ≥ MeaningfulSessionMinEvaluatedActivities evaluated activities):
/// Bootstrap before BootstrapBeforeSessions, Ramp until AdaptiveAfterSessions, then Adaptive; Adaptive also starts after
/// EarlyAdaptiveMinSessions when total evidence ≥ EarlyAdaptiveMinEvidence.
/// </summary>
public sealed record ColdStartRules(
    int MeaningfulSessionMinEvaluatedActivities,
    int BootstrapBeforeSessions,
    decimal BootstrapNewShare,
    int AdaptiveAfterSessions,
    decimal RampNewShare,
    int EarlyAdaptiveMinSessions,
    int EarlyAdaptiveMinEvidence)
{
    /// <summary>Bootstrap phase: fewer than <see cref="BootstrapBeforeSessions"/> meaningful sessions (the V1 "newbie").</summary>
    public bool IsBootstrap(int meaningfulSessionCount) => Guard.NotNegative(meaningfulSessionCount) < BootstrapBeforeSessions;

    /// <summary>
    /// Cold-start phase (ASE §25, §48–49): a completed placement starts in Adaptive; otherwise Bootstrap below
    /// <see cref="BootstrapBeforeSessions"/> meaningful sessions, Adaptive from <see cref="AdaptiveAfterSessions"/> or from
    /// <see cref="EarlyAdaptiveMinSessions"/> with at least <see cref="EarlyAdaptiveMinEvidence"/> evidence, else Ramp.
    /// </summary>
    public ColdStartPhase PhaseOf(int meaningfulSessionCount, int totalEvidenceCount, bool placementCompleted)
    {
        Guard.NotNegative(totalEvidenceCount);
        if (placementCompleted)
        {
            return ColdStartPhase.Adaptive;
        }

        if (IsBootstrap(meaningfulSessionCount))
        {
            return ColdStartPhase.Bootstrap;
        }

        return meaningfulSessionCount >= AdaptiveAfterSessions
            || (meaningfulSessionCount >= EarlyAdaptiveMinSessions && totalEvidenceCount >= EarlyAdaptiveMinEvidence)
            ? ColdStartPhase.Adaptive
            : ColdStartPhase.Ramp;
    }
}

/// <summary>Diversity rules D1–D4 (ASE §31).</summary>
public sealed record DiversityRules(int MaxSameTypeInARow, int MinTypesFromFiveMinutes, int MinTypesFromTenMinutes, int MaxSameKnowledgeInARow);
