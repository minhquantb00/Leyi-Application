using Leyi.Domain.Common;
using Leyi.Domain.Knowledge;
using Leyi.Domain.Learning;
using Leyi.Domain.LearningProfile;
using Leyi.Domain.Mastery;
using Leyi.Domain.Sessions.Scoring;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>
/// Already-loaded learning context of one learner for <see cref="CandidateBuilder"/> (ASE §56). Neutral values only — no
/// entities, no content payload. Loading it (current lesson scope, recent-error aggregates, content capabilities) is the
/// caller's job.
/// </summary>
public sealed record CandidateBuildContext
{
    public CandidateBuildContext(
        DateTime nowUtc,
        DateTime nextLocalDayStartUtc,
        PrimaryGoal primaryGoal,
        IReadOnlyCollection<MasteryDimension> weakSkillDimensions,
        IReadOnlyCollection<KnowledgeLearningState> knowledgeUnits)
    {
        NowUtc = Guard.Utc(nowUtc);
        NextLocalDayStartUtc = Guard.Utc(nextLocalDayStartUtc);
        if (nextLocalDayStartUtc <= nowUtc)
        {
            throw new ArgumentException("The next local day starts after now.", nameof(nextLocalDayStartUtc));
        }

        PrimaryGoal = Guard.Defined(primaryGoal);
        ArgumentNullException.ThrowIfNull(weakSkillDimensions);
        ArgumentNullException.ThrowIfNull(knowledgeUnits);
        WeakSkillDimensions = weakSkillDimensions.Select(dimension => Guard.Defined(dimension)).Distinct().Order().ToList();
        if (knowledgeUnits.Select(unit => unit.KnowledgeUnitId).Distinct().Count() != knowledgeUnits.Count)
        {
            throw new ArgumentException("Each knowledge unit may appear only once.", nameof(knowledgeUnits));
        }

        KnowledgeUnits = knowledgeUnits.ToList();
    }

    public DateTime NowUtc { get; }

    /// <summary>End (exclusive) of the learner's local day, from <c>LearnerLocalDateResolver</c>.</summary>
    public DateTime NextLocalDayStartUtc { get; }

    public PrimaryGoal PrimaryGoal { get; }

    /// <summary>User-level weak skills from <see cref="Policies.SessionPolicy.WeakSkills"/> (ASE §54); tags candidates for the planner's soft target.</summary>
    public IReadOnlyList<MasteryDimension> WeakSkillDimensions { get; }

    public IReadOnlyList<KnowledgeLearningState> KnowledgeUnits { get; }
}

/// <summary>
/// One knowledge unit the learner may practise. <paramref name="Mastery"/> null = NEW (no mastery row).
/// <paramref name="GeneratableActivityTypes"/> = assessment types whose content can be generated (decided by the content
/// side); NEW_VOCABULARY_EXPOSURE is not an assessment capability.
/// </summary>
public sealed record KnowledgeLearningState
{
    public KnowledgeLearningState(
        Guid knowledgeUnitId,
        KnowledgeType knowledgeType,
        bool isActive,
        int contentVersion,
        CurriculumPosition curriculumPosition,
        int? lessonDistanceFromCurrent,
        int? lessonItemSortOrder,
        KnowledgeMasteryState? mastery,
        IReadOnlyCollection<ActivityType> generatableActivityTypes)
    {
        KnowledgeUnitId = Guard.NotEmpty(knowledgeUnitId);
        KnowledgeType = Guard.Defined(knowledgeType);
        IsActive = isActive;
        ContentVersion = Guard.Positive(contentVersion);
        CurriculumPosition = Guard.Defined(curriculumPosition);
        if (lessonDistanceFromCurrent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lessonDistanceFromCurrent), lessonDistanceFromCurrent, "A reached lesson is never ahead of the current one.");
        }

        if ((curriculumPosition == CurriculumPosition.CurrentLesson) != (lessonDistanceFromCurrent == 0))
        {
            throw new ArgumentException("The current lesson, and only it, is a reached lesson at distance 0.", nameof(lessonDistanceFromCurrent));
        }

        if (curriculumPosition == CurriculumPosition.NextLesson && lessonDistanceFromCurrent is not null)
        {
            throw new ArgumentException("A future lesson is never reached: it has no lesson distance and never enters the fallback.", nameof(lessonDistanceFromCurrent));
        }

        LessonDistanceFromCurrent = lessonDistanceFromCurrent;
        LessonItemSortOrder = lessonItemSortOrder;
        Mastery = mastery;
        ArgumentNullException.ThrowIfNull(generatableActivityTypes);
        GeneratableActivityTypes = generatableActivityTypes
            .Select(type => Guard.Defined(type).IsAssessment()
                ? type
                : throw new ArgumentException($"{type} is not an assessment activity capability.", nameof(generatableActivityTypes)))
            .Distinct()
            .Order()
            .ToList();
    }

    public Guid KnowledgeUnitId { get; }

    public KnowledgeType KnowledgeType { get; }

    /// <summary>
    /// Knowledge unit status is ACTIVE. Required for NEW content only; a learned (mastered) unit stays reviewable when
    /// ARCHIVED (AS-G16).
    /// </summary>
    public bool IsActive { get; }

    public int ContentVersion { get; }

    public CurriculumPosition CurriculumPosition { get; }

    /// <summary>
    /// Reached lessons back from the current one, for a KU in a lesson of the current curriculum level whose SortOrder is at
    /// most the current lesson's: current lesson = 0, the previous reached lesson = 1, then 2, 3, … (AS-G19). Null outside
    /// that scope (other level, future lesson, not placed). Computed by the context loader; never a future lesson.
    /// </summary>
    public int? LessonDistanceFromCurrent { get; }

    /// <summary>In the fallback scope of AS-G10 (a reached lesson of the current level, the current lesson included).</summary>
    public bool IsInReachedLessonOfCurrentLevel => LessonDistanceFromCurrent is not null;

    /// <summary><c>LessonKnowledgeUnits.SortOrder</c> in the lesson that places the KU (tie-break); null when not placed.</summary>
    public int? LessonItemSortOrder { get; }

    public KnowledgeMasteryState? Mastery { get; }

    public IReadOnlyList<ActivityType> GeneratableActivityTypes { get; }
}

/// <summary>Current mastery and review state of a knowledge unit the learner has met.</summary>
public sealed record KnowledgeMasteryState
{
    public KnowledgeMasteryState(
        KnowledgeState knowledgeState,
        ConfidenceLevel confidence,
        DateTime firstSeenAtUtc,
        ReviewLevel reviewLevel,
        DateTime nextReviewAtUtc,
        IReadOnlyCollection<DimensionLearningState> dimensions)
    {
        KnowledgeState = Guard.Defined(knowledgeState);
        Confidence = Guard.Defined(confidence);
        FirstSeenAtUtc = Guard.Utc(firstSeenAtUtc);
        ReviewLevel = reviewLevel;
        NextReviewAtUtc = Guard.Utc(nextReviewAtUtc);
        ArgumentNullException.ThrowIfNull(dimensions);
        if (dimensions.Select(dimension => dimension.Dimension).Distinct().Count() != dimensions.Count)
        {
            throw new ArgumentException("Each dimension may appear only once.", nameof(dimensions));
        }

        Dimensions = dimensions.OrderBy(dimension => dimension.Dimension).ToList();
    }

    public KnowledgeState KnowledgeState { get; }

    public ConfidenceLevel Confidence { get; }

    public DateTime FirstSeenAtUtc { get; }

    public ReviewLevel ReviewLevel { get; }

    public DateTime NextReviewAtUtc { get; }

    public IReadOnlyList<DimensionLearningState> Dimensions { get; }

    /// <summary>
    /// The dimension's state. A mastery always has its knowledge type's dimension rows (B8, checked by
    /// <see cref="CandidateBuilder"/>); a missing one is an error, never synthesized as unmeasured.
    /// </summary>
    public DimensionLearningState For(MasteryDimension dimension) =>
        Dimensions.FirstOrDefault(state => state.Dimension == dimension)
        ?? throw new InvalidOperationException($"The mastery has no {dimension} dimension row (B8).");
}

/// <summary>One dimension's mastery: score, evidence count, and the recent errors (RecentErrorCount, aggregated by the caller).</summary>
public sealed record DimensionLearningState
{
    public DimensionLearningState(MasteryDimension dimension, decimal score, int evidenceCount, int recentErrors)
    {
        Dimension = Guard.Defined(dimension);
        Score = Guard.InRange(score, 0m, 100m);
        EvidenceCount = Guard.NotNegative(evidenceCount);
        RecentErrors = Guard.NotNegative(recentErrors);
    }

    public MasteryDimension Dimension { get; }

    public decimal Score { get; }

    public int EvidenceCount { get; }

    /// <summary>
    /// Transient recent-error evidence count: Q0 evidence of the 14-day window (LM §43), aggregated from LearningEvidence per
    /// request — never persisted (B7).
    /// </summary>
    public int RecentErrors { get; }

    /// <summary>
    /// The scoring input of a dimension with no evidence. Used only for a NEW knowledge unit (no mastery, ASE §10); never a
    /// stand-in for a missing row of an existing mastery.
    /// </summary>
    public static DimensionLearningState Unmeasured(MasteryDimension dimension) => new(dimension, 0m, 0, 0);
}
