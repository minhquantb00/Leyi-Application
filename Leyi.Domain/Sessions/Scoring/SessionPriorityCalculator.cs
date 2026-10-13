using Leyi.Domain.Common;
using Leyi.Domain.Learning;
using Leyi.Domain.LearningProfile;
using Leyi.Domain.Mastery;
using Leyi.Domain.Policies;

namespace Leyi.Domain.Sessions.Scoring;

/// <summary>
/// SessionPriorityCalculator (Domain Model §13, ASE §11–21, E1, AS-G15): pure, deterministic scoring of adaptive-session
/// candidates from already-loaded values. Signal methods turn state into the five 0–100 priority signals;
/// <see cref="Score"/> combines them into <c>FinalScore = P × EvidenceValue × GoalModifier × ConfidenceModifier</c> and
/// explains it with additive contributions. Every number comes from the versioned policies; decimals throughout.
/// It neither selects nor ranks candidates (SessionPlanner) nor reads data.
/// </summary>
public sealed class SessionPriorityCalculator
{
    /// <summary>Multiplicative identity: a modifier that does not change the score.</summary>
    private const decimal NeutralModifier = 1m;

    private readonly SessionPolicy sessionPolicy;
    private readonly GoalPolicy goalPolicy;
    private readonly EvidencePolicy evidencePolicy;
    private readonly MasteryPolicy masteryPolicy;
    private readonly SrsPolicy srsPolicy;

    public SessionPriorityCalculator(SessionPolicy sessionPolicy, GoalPolicy goalPolicy, EvidencePolicy evidencePolicy, MasteryPolicy masteryPolicy, SrsPolicy srsPolicy)
    {
        this.sessionPolicy = sessionPolicy ?? throw new ArgumentNullException(nameof(sessionPolicy));
        this.goalPolicy = goalPolicy ?? throw new ArgumentNullException(nameof(goalPolicy));
        this.evidencePolicy = evidencePolicy ?? throw new ArgumentNullException(nameof(evidencePolicy));
        this.masteryPolicy = masteryPolicy ?? throw new ArgumentNullException(nameof(masteryPolicy));
        this.srsPolicy = srsPolicy ?? throw new ArgumentNullException(nameof(srsPolicy));
    }

    public static SessionPriorityCalculator V1 { get; } = new(SessionPolicy.V1, GoalPolicy.V1, EvidencePolicy.V1, MasteryPolicy.V1, SrsPolicy.V1);

    /// <summary>
    /// Review timing (LM §22.1, ASE §7): level 0 → none; due at or before now → DueNow, or Overdue when
    /// <c>(now − NextReviewAt) / interval(level)</c> reaches the overdue threshold; due before the next local day → DueToday.
    /// </summary>
    public ReviewTiming ClassifyReview(ReviewLevel reviewLevel, DateTime nextReviewAtUtc, DateTime nowUtc, DateTime nextLocalDayStartUtc)
    {
        Guard.Utc(nextReviewAtUtc);
        Guard.Utc(nowUtc);
        Guard.Utc(nextLocalDayStartUtc);

        if (!reviewLevel.IsInReviewCycle)
        {
            return ReviewTiming.NotDue;
        }

        if (nextReviewAtUtc <= nowUtc)
        {
            var overdueRatio = (decimal)(nowUtc - nextReviewAtUtc).Ticks / srsPolicy.IntervalFor(reviewLevel).Ticks;
            var status = overdueRatio >= sessionPolicy.OverdueThresholdRatio ? ReviewDueStatus.Overdue : ReviewDueStatus.DueNow;
            return new ReviewTiming(status, overdueRatio);
        }

        return nextReviewAtUtc < nextLocalDayStartUtc ? new ReviewTiming(ReviewDueStatus.DueToday, 0m) : ReviewTiming.NotDue;
    }

    /// <summary>
    /// ReviewUrgency (ASE §13): none → 0; DueToday → DueToday score; due → DueNow score at OverdueRatio 0, rising linearly
    /// to the one-interval score at ratio 1 and to the two-interval score at ratio 2, then constant.
    /// </summary>
    public decimal ReviewUrgency(ReviewTiming timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        var scores = sessionPolicy.ReviewUrgency;
        return Guard.Defined(timing.Status) switch
        {
            ReviewDueStatus.None => 0m,
            ReviewDueStatus.DueToday => scores.DueToday,
            _ => timing.OverdueRatio switch
            {
                < 0m => throw new ArgumentOutOfRangeException(nameof(timing), timing.OverdueRatio, "A due item cannot have a negative overdue ratio."),
                < 1m => scores.DueNow + ((scores.OneIntervalOverdue - scores.DueNow) * timing.OverdueRatio),
                < 2m => scores.OneIntervalOverdue + ((scores.TwoIntervalsOverdue - scores.OneIntervalOverdue) * (timing.OverdueRatio - 1m)),
                _ => scores.TwoIntervalsOverdue,
            },
        };
    }

    /// <summary>
    /// DimensionWeakness (ASE §10, §14): an unmeasured dimension (no evidence, including a NEW KU) scores the unmeasured
    /// weakness; otherwise <c>min(100, max(0, Target − Score) × points per gap point)</c>.
    /// </summary>
    public decimal DimensionWeakness(MasteryDimension dimension, decimal score, int evidenceCount)
    {
        Guard.InRange(score, 0m, 100m);
        if (Guard.NotNegative(evidenceCount) == 0)
        {
            return sessionPolicy.UnmeasuredDimensionWeakness;
        }

        var target = masteryPolicy.DimensionTargets.TryGetValue(Guard.Defined(dimension), out var value)
            ? value
            : throw new InvalidOperationException($"Mastery policy {masteryPolicy.Version} defines no target for {dimension}.");
        var gap = Math.Max(0m, target - score);
        return Math.Min(100m, gap * sessionPolicy.WeaknessScorePerGapPoint);
    }

    /// <summary>RecentError (ASE §15): score by recent error count; the last configured score applies to higher counts.</summary>
    public decimal RecentError(int recentErrorCount)
    {
        var scores = sessionPolicy.RecentErrorScores;
        return scores[Math.Min(Guard.NotNegative(recentErrorCount), scores.Count - 1)];
    }

    /// <summary>CurriculumRelevance (ASE §16) by position relative to the current lesson.</summary>
    public decimal CurriculumRelevance(CurriculumPosition position)
    {
        var scores = sessionPolicy.CurriculumRelevance;
        return Guard.Defined(position) switch
        {
            CurriculumPosition.CurrentLesson => scores.CurrentLesson,
            CurriculumPosition.NextLesson => scores.NextLesson,
            CurriculumPosition.PreviousLesson => scores.PreviousLesson,
            _ => scores.OtherLesson,
        };
    }

    /// <summary>
    /// ReinforcementNeed (ASE §18) by time since the knowledge was learned: within the recent window, within a day, or
    /// older. Null (the caller has no reinforcement signal for this candidate) → 0.
    /// </summary>
    public decimal ReinforcementNeed(TimeSpan? sinceLearned)
    {
        if (sinceLearned is not { } elapsed)
        {
            return 0m;
        }

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sinceLearned), elapsed, "Elapsed time cannot be negative.");
        }

        var windows = sessionPolicy.ReinforcementWindows;
        var scores = sessionPolicy.Reinforcement;
        return elapsed < windows.Recent ? scores.WithinTenMinutes
            : elapsed < windows.Day ? scores.WithinDay
            : scores.Older;
    }

    /// <summary>
    /// ConfidenceModifier (ASE §19, §51): applied only to knowledge already in the review cycle (level ≥ 1); a NEW KU or a
    /// level-0 item is not modified.
    /// </summary>
    public decimal ConfidenceModifier(ReviewLevel? reviewLevel, ConfidenceLevel? confidence)
    {
        if (reviewLevel is not { IsInReviewCycle: true })
        {
            return NeutralModifier;
        }

        if (confidence is not { } level)
        {
            throw new ArgumentException("Knowledge in the review cycle always has a confidence level.", nameof(confidence));
        }

        return sessionPolicy.ConfidenceModifiers.TryGetValue(Guard.Defined(level), out var modifier)
            ? modifier
            : throw new InvalidOperationException($"Session policy {sessionPolicy.Version} defines no confidence modifier for {level}.");
    }

    /// <summary>PriorityScore (ASE §11): the weighted sum of the five signals, 0–100.</summary>
    public decimal PriorityScore(PriorityComponents components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var weights = sessionPolicy.PriorityWeights;
        return (weights.ReviewUrgency * components.ReviewUrgency)
            + (weights.DimensionWeakness * components.DimensionWeakness)
            + (weights.RecentError * components.RecentError)
            + (weights.CurriculumRelevance * components.CurriculumRelevance)
            + (weights.ReinforcementNeed * components.ReinforcementNeed);
    }

    /// <summary>
    /// FinalScore and its explanation (ASE §19, AS-G15) for one assessment candidate. Each non-zero signal contributes
    /// <c>wᵢ × signalᵢ × EV</c>; a goal modifier above 1 contributes <c>P × EV × (GM − 1)</c> (GOAL_RELEVANCE); a confidence
    /// modifier above 1 contributes <c>P × EV × GM × (CM − 1)</c> (LOW_CONFIDENCE). Zero-valued contributions are omitted.
    /// The contributions add up to FinalScore.
    /// A non-assessment activity is not scored (a NEW bundle ranks by its paired recall).
    /// </summary>
    public CandidateScore Score(CandidateScoringInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!Guard.Defined(input.ActivityType).IsAssessment())
        {
            throw new ArgumentException($"{input.ActivityType} is not an assessment activity and is not scored.", nameof(input));
        }

        // A review status goes with review urgency and only then: the reason label comes from the typed status.
        var hasReviewStatus = Guard.Defined(input.ReviewStatus) != ReviewDueStatus.None;
        if (hasReviewStatus != (input.Components.ReviewUrgency > 0m))
        {
            throw new ArgumentException($"Review status {input.ReviewStatus} does not match review urgency {input.Components.ReviewUrgency}.", nameof(input));
        }

        var rule = evidencePolicy.RuleFor(input.ActivityType);
        var components = input.Components;
        var evidenceValue = rule.EvidenceWeight;
        var goalModifier = goalPolicy.ModifierFor(input.PrimaryGoal, rule.PrimaryDimension);
        var confidenceModifier = ConfidenceModifier(input.ReviewLevel, input.Confidence);
        var priorityScore = PriorityScore(components);
        var weightedPriority = priorityScore * evidenceValue;
        var finalScore = weightedPriority * goalModifier * confidenceModifier;

        var weights = sessionPolicy.PriorityWeights;
        var contributions = new List<ScoreContribution>();
        if (hasReviewStatus)
        {
            AddSignal(ReviewReasonOf(input.ReviewStatus), weights.ReviewUrgency, components.ReviewUrgency);
        }

        AddSignal(SelectionReason.WeakDimension, weights.DimensionWeakness, components.DimensionWeakness);
        AddSignal(SelectionReason.RecentError, weights.RecentError, components.RecentError);
        AddSignal(SelectionReason.Curriculum, weights.CurriculumRelevance, components.CurriculumRelevance);
        AddSignal(SelectionReason.Reinforcement, weights.ReinforcementNeed, components.ReinforcementNeed);

        if (weightedPriority > 0m && goalModifier > NeutralModifier)
        {
            contributions.Add(new ScoreContribution(SelectionReason.GoalRelevance, weightedPriority * (goalModifier - NeutralModifier)));
        }

        if (weightedPriority > 0m && confidenceModifier > NeutralModifier)
        {
            contributions.Add(new ScoreContribution(SelectionReason.LowConfidence, weightedPriority * goalModifier * (confidenceModifier - NeutralModifier)));
        }

        return new CandidateScore(priorityScore, evidenceValue, goalModifier, confidenceModifier, finalScore, contributions);

        void AddSignal(SelectionReason reason, decimal weight, decimal signal)
        {
            if (signal > 0m)
            {
                contributions.Add(new ScoreContribution(reason, weight * signal * evidenceValue));
            }
        }
    }

    /// <summary>Review reason of a review status (AS-G15): DueNow and DueToday are DUE_REVIEW, Overdue is OVERDUE.</summary>
    private static SelectionReason ReviewReasonOf(ReviewDueStatus status) => status switch
    {
        ReviewDueStatus.DueToday or ReviewDueStatus.DueNow => SelectionReason.DueReview,
        ReviewDueStatus.Overdue => SelectionReason.Overdue,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Only a due status has a review reason."),
    };
}
