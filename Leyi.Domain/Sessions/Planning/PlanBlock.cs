using Leyi.Domain.Learning;
using Leyi.Domain.Policies;
using Leyi.Domain.Sessions.Scoring;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>One activity inside a <see cref="PlanBlock"/>.</summary>
public sealed record PlanBlockActivity(
    Guid KnowledgeUnitId,
    ActivityType ActivityType,
    MasteryDimension? PrimaryDimension,
    PlannedActivityRole Role,
    int ContentVersion,
    int DurationSeconds,
    CandidateScore? Score,
    IReadOnlyList<ActivitySelectionReason> Reasons);

/// <summary>
/// The unit the planner selects and the orderer moves: one assessment candidate, or an atomic NEW bundle
/// [NEW_VOCABULARY_EXPOSURE, Recall] that is always selected and placed whole (AS-G7).
/// </summary>
public sealed class PlanBlock
{
    private PlanBlock(CandidateRankKey rankKey, IReadOnlyList<PlanBlockActivity> activities, bool isNew, bool isCoreReview, bool isSecondaryOnly, int weakSkillSeconds)
    {
        RankKey = rankKey;
        Activities = activities;
        IsNew = isNew;
        IsCoreReview = isCoreReview;
        IsSecondaryOnly = isSecondaryOnly;
        WeakSkillSeconds = weakSkillSeconds;
        Cost = activities.Sum(activity => activity.DurationSeconds);
        KnowledgeUnitId = activities[0].KnowledgeUnitId;
        AssessmentDimensions = activities.Where(activity => activity.PrimaryDimension is not null).Select(activity => activity.PrimaryDimension!.Value).ToList();
        AssessmentTypes = activities.Where(activity => activity.ActivityType.IsAssessment()).Select(activity => activity.ActivityType).ToList();
    }

    /// <summary>
    /// Selection reasons of a NEW_VOCABULARY_EXPOSURE (AS-G20): NEW curriculum content that was not assessment-scored —
    /// exactly CURRICULUM with no score contribution. The paired recall's reasons are never copied onto it.
    /// </summary>
    public static IReadOnlyList<ActivitySelectionReason> ExposureReasons { get; } = [new ActivitySelectionReason(SelectionReason.Curriculum, ScoreContribution: null)];

    public CandidateRankKey RankKey { get; }

    public IReadOnlyList<PlanBlockActivity> Activities { get; }

    /// <summary>A NEW bundle (counts against the new-content cap).</summary>
    public bool IsNew { get; }

    /// <summary>A Pool A/B review (core review slot).</summary>
    public bool IsCoreReview { get; }

    /// <summary>Only due later today (secondary pass).</summary>
    public bool IsSecondaryOnly { get; }

    /// <summary>
    /// Seconds counted toward the soft weak-skill target (AS-G18): estimated seconds of its activities whose primary dimension
    /// is a user-level weak skill. For a NEW bundle only the paired recall can count; the exposure has no dimension.
    /// </summary>
    public int WeakSkillSeconds { get; }

    public bool IsWeakSkill => WeakSkillSeconds > 0;

    /// <summary>Estimated seconds of all its activities.</summary>
    public int Cost { get; }

    public Guid KnowledgeUnitId { get; }

    /// <summary>Primary dimensions of its assessments (D5).</summary>
    public IReadOnlyList<MasteryDimension> AssessmentDimensions { get; }

    /// <summary>Assessment activity types (D2/D3 count; exposure excluded).</summary>
    public IReadOnlyList<ActivityType> AssessmentTypes { get; }

    public static PlanBlock FromAssessment(AdaptiveCandidate candidate, SessionPolicy sessionPolicy)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(sessionPolicy);
        var activity = new PlanBlockActivity(
            candidate.KnowledgeUnitId,
            candidate.ActivityType,
            candidate.PrimaryDimension,
            PlannedActivityRole.Assessment,
            candidate.ContentVersion,
            DurationOf(candidate.ActivityType, sessionPolicy),
            candidate.Score,
            candidate.Score.ToSelectionReasons());
        return new PlanBlock(candidate.RankKey, [activity], isNew: false, candidate.IsCoreReview, candidate.IsSecondaryOnly, candidate.IsWeakSkillDimension ? activity.DurationSeconds : 0);
    }

    public static PlanBlock FromNewBundle(NewVocabularyCandidate candidate, SessionPolicy sessionPolicy, EvidencePolicy evidencePolicy)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(sessionPolicy);
        ArgumentNullException.ThrowIfNull(evidencePolicy);
        var exposure = new PlanBlockActivity(
            candidate.KnowledgeUnitId,
            ActivityType.NewVocabularyExposure,
            PrimaryDimension: null,
            PlannedActivityRole.NewExposure,
            candidate.ContentVersion,
            DurationOf(ActivityType.NewVocabularyExposure, sessionPolicy),
            Score: null,
            ExposureReasons);
        var recall = new PlanBlockActivity(
            candidate.KnowledgeUnitId,
            candidate.RecallActivityType,
            evidencePolicy.RuleFor(candidate.RecallActivityType).PrimaryDimension,
            PlannedActivityRole.NewRecall,
            candidate.ContentVersion,
            DurationOf(candidate.RecallActivityType, sessionPolicy),
            candidate.RecallScore,
            candidate.RecallScore.ToSelectionReasons());
        return new PlanBlock(candidate.RankKey, [exposure, recall], isNew: true, isCoreReview: false, isSecondaryOnly: false, candidate.IsWeakSkillDimension ? recall.DurationSeconds : 0);
    }

    /// <summary>Planning estimate of an activity type (ASE §6) in whole seconds, from <see cref="SessionPolicy.ActivityDurations"/>.</summary>
    public static int DurationOf(ActivityType activityType, SessionPolicy sessionPolicy)
    {
        ArgumentNullException.ThrowIfNull(sessionPolicy);
        return sessionPolicy.ActivityDurations.TryGetValue(activityType, out var duration)
            ? checked((int)(duration.Ticks / TimeSpan.TicksPerSecond))
            : throw new InvalidOperationException($"Session policy {sessionPolicy.Version} has no duration for {activityType}.");
    }
}
