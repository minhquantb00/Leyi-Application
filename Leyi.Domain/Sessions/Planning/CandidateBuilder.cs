using Leyi.Domain.Common;
using Leyi.Domain.Knowledge;
using Leyi.Domain.Learning;
using Leyi.Domain.Mastery;
using Leyi.Domain.Policies;
using Leyi.Domain.Sessions.Scoring;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>
/// CandidateBuilder (Domain Model §13, ASE §7–19, AS-G4, AS-G6, AS-G7): turns an already-loaded learning context into
/// scored candidates. Pool membership, one candidate per (KU, generatable assessment ActivityType) with merged
/// signals/reasons, NEW vocabulary bundle candidates, and deterministic ranking keys. It does not budget, cap new content,
/// apply diversity, fall back, order the plan or touch content (SessionPlanner / ActivityContentFactory).
/// Vocabulary only in V1; grammar knowledge units are skipped (not errors).
/// </summary>
/// <remarks>
/// <para>
/// Lifecycle (AS-G16): ACTIVE is required for NEW content (Pool E) only. A previously learned knowledge unit (one with
/// mastery) stays reviewable after it is ARCHIVED — Pools A/B/C/D/F and DueToday — like the ReviewDebt and GET /today
/// counts, as long as an assessment ActivityType can still be generated.
/// </para>
/// <para>
/// Reinforcement (ASE §7 Pool F, §18, AS-G17): "vừa học" is measured from <c>FirstSeenAt</c> (the Pool F level-1 rule is
/// defined on it). The ReinforcementNeed signal is suppressed when the KU has a stronger signal — DueNow, Overdue, WEAK or
/// a recent error; DueToday is not one. Pool F membership is recorded either way.
/// </para>
/// </remarks>
public sealed class CandidateBuilder
{
    private const CandidatePools StrongSignals = CandidatePools.DueNow | CandidatePools.Overdue | CandidatePools.Weak | CandidatePools.RecentError;

    private readonly SessionPriorityCalculator calculator;
    private readonly SessionPolicy sessionPolicy;
    private readonly EvidencePolicy evidencePolicy;
    private readonly MasteryPolicy masteryPolicy;

    public CandidateBuilder(SessionPriorityCalculator calculator, SessionPolicy sessionPolicy, EvidencePolicy evidencePolicy, MasteryPolicy masteryPolicy)
    {
        this.calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        this.sessionPolicy = sessionPolicy ?? throw new ArgumentNullException(nameof(sessionPolicy));
        this.evidencePolicy = evidencePolicy ?? throw new ArgumentNullException(nameof(evidencePolicy));
        this.masteryPolicy = masteryPolicy ?? throw new ArgumentNullException(nameof(masteryPolicy));
    }

    public static CandidateBuilder V1 { get; } = new(SessionPriorityCalculator.V1, SessionPolicy.V1, EvidencePolicy.V1, MasteryPolicy.V1);

    public CandidateSet Build(CandidateBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var unit in context.KnowledgeUnits)
        {
            EnsureCompleteDimensions(unit);
        }

        var assessments = new List<AdaptiveCandidate>();
        var newVocabulary = new List<NewVocabularyCandidate>();
        var fallback = new List<FallbackCandidate>();
        foreach (var unit in context.KnowledgeUnits)
        {
            if (unit.KnowledgeType != KnowledgeType.Vocabulary)
            {
                continue;
            }

            if (unit.Mastery is { } mastery)
            {
                var timing = calculator.ClassifyReview(mastery.ReviewLevel, mastery.NextReviewAtUtc, context.NowUtc, context.NextLocalDayStartUtc);
                var pools = PoolsOf(timing.Status, mastery, context.NowUtc);
                var candidates = pools != CandidatePools.None || unit.IsInReachedLessonOfCurrentLevel
                    ? ScoreAssessments(context, unit, mastery, timing, pools)
                    : [];
                if (pools != CandidatePools.None)
                {
                    assessments.AddRange(candidates);
                }

                if (unit.LessonDistanceFromCurrent is { } distance)
                {
                    fallback.AddRange(candidates.Select(candidate => new FallbackCandidate(candidate, mastery.KnowledgeState, distance)));
                }
            }
            else if (unit.IsActive && BuildNewVocabulary(context, unit) is { } candidate)
            {
                newVocabulary.Add(candidate);
            }
        }

        return new CandidateSet(
            assessments.OrderBy(candidate => candidate.RankKey, CandidateRanking.Instance).ToList(),
            newVocabulary.OrderBy(candidate => candidate.RankKey, CandidateRanking.Instance).ToList(),
            fallback.OrderBy(candidate => candidate.LessonDistanceFromCurrent).ThenBy(candidate => candidate.Candidate.RankKey, CandidateRanking.Instance).ToList(),
            HasWeakSkillDimensions: context.WeakSkillDimensions.Count > 0);
    }

    /// <summary>One scored candidate per generatable assessment type of a learned KU (normal pools or fallback).</summary>
    private List<AdaptiveCandidate> ScoreAssessments(CandidateBuildContext context, KnowledgeLearningState unit, KnowledgeMasteryState mastery, ReviewTiming timing, CandidatePools pools)
    {
        var reviewUrgency = calculator.ReviewUrgency(timing);
        var reinforcementNeed = (pools & CandidatePools.Reinforcement) != 0 && (pools & StrongSignals) == 0
            ? calculator.ReinforcementNeed(context.NowUtc - mastery.FirstSeenAtUtc)
            : 0m;
        var curriculumRelevance = calculator.CurriculumRelevance(unit.CurriculumPosition);

        return unit.GeneratableActivityTypes.Select(activityType =>
        {
            var dimension = evidencePolicy.RuleFor(activityType).PrimaryDimension;
            var dimensionState = mastery.For(dimension);
            var components = new PriorityComponents(
                reviewUrgency,
                calculator.DimensionWeakness(dimension, dimensionState.Score, dimensionState.EvidenceCount),
                calculator.RecentError(dimensionState.RecentErrors),
                curriculumRelevance,
                reinforcementNeed);
            var score = calculator.Score(new CandidateScoringInput(components, activityType, context.PrimaryGoal, mastery.ReviewLevel, mastery.Confidence, timing.Status));

            return new AdaptiveCandidate(
                unit.KnowledgeUnitId,
                activityType,
                dimension,
                pools,
                timing.Status,
                context.WeakSkillDimensions.Contains(dimension),
                unit.ContentVersion,
                score,
                RankKey(score, mastery.NextReviewAtUtc, curriculumRelevance, unit, activityType));
        }).ToList();
    }

    /// <summary>
    /// Pool E (AS-G7): a NEW vocabulary of the current lesson whose paired Recall can be generated — MEANING_TO_HANZI
    /// preferred, MEANING_TO_TYPE_HANZI otherwise — ranked by that Recall's score (unmeasured Recall, current lesson,
    /// no review/error/reinforcement signal, neutral confidence).
    /// </summary>
    private NewVocabularyCandidate? BuildNewVocabulary(CandidateBuildContext context, KnowledgeLearningState unit)
    {
        if (unit.CurriculumPosition != CurriculumPosition.CurrentLesson)
        {
            return null;
        }

        ActivityType? recallActivityType = unit.GeneratableActivityTypes.Contains(ActivityType.MeaningToHanzi) ? ActivityType.MeaningToHanzi
            : unit.GeneratableActivityTypes.Contains(ActivityType.MeaningToTypeHanzi) ? ActivityType.MeaningToTypeHanzi
            : null;
        if (recallActivityType is not { } activityType)
        {
            return null;
        }

        var dimension = evidencePolicy.RuleFor(activityType).PrimaryDimension;
        var curriculumRelevance = calculator.CurriculumRelevance(unit.CurriculumPosition);
        var unmeasured = DimensionLearningState.Unmeasured(dimension);
        var components = new PriorityComponents(
            reviewUrgency: 0m,
            dimensionWeakness: calculator.DimensionWeakness(dimension, unmeasured.Score, unmeasured.EvidenceCount),
            recentError: 0m,
            curriculumRelevance: curriculumRelevance,
            reinforcementNeed: 0m);
        var score = calculator.Score(new CandidateScoringInput(components, activityType, context.PrimaryGoal, ReviewLevel: null, Confidence: null, ReviewDueStatus.None));

        return new NewVocabularyCandidate(
            unit.KnowledgeUnitId,
            activityType,
            context.WeakSkillDimensions.Contains(dimension),
            unit.ContentVersion,
            score,
            RankKey(score, nextReviewAtUtc: null, curriculumRelevance, unit, activityType));
    }

    /// <summary>Pool membership of a known knowledge unit (ASE §7).</summary>
    private CandidatePools PoolsOf(ReviewDueStatus reviewStatus, KnowledgeMasteryState mastery, DateTime nowUtc)
    {
        var pools = Guard.Defined(reviewStatus) switch
        {
            ReviewDueStatus.DueNow => CandidatePools.DueNow,
            ReviewDueStatus.Overdue => CandidatePools.Overdue,
            ReviewDueStatus.DueToday => CandidatePools.DueToday,
            _ => CandidatePools.None,
        };

        if (mastery.KnowledgeState == KnowledgeState.Weak)
        {
            pools |= CandidatePools.Weak;
        }

        if (mastery.Dimensions.Any(dimension => dimension.RecentErrors > 0))
        {
            pools |= CandidatePools.RecentError;
        }

        if (IsReinforcementEligible(reviewStatus, mastery, nowUtc))
        {
            pools |= CandidatePools.Reinforcement;
        }

        return pools;
    }

    /// <summary>Pool F (ASE §7): level 0 due, or level 1 first seen within a day and not due (not DueNow/Overdue).</summary>
    private bool IsReinforcementEligible(ReviewDueStatus reviewStatus, KnowledgeMasteryState mastery, DateTime nowUtc)
    {
        var sinceFirstSeen = nowUtc - mastery.FirstSeenAtUtc;
        if (sinceFirstSeen < TimeSpan.Zero)
        {
            throw new ArgumentException("A knowledge unit cannot be first seen after now.", nameof(mastery));
        }

        return mastery.ReviewLevel.Value switch
        {
            ReviewLevel.MinValue => mastery.NextReviewAtUtc <= nowUtc,
            ReviewLevel.MinValue + 1 => reviewStatus is not (ReviewDueStatus.DueNow or ReviewDueStatus.Overdue)
                && sinceFirstSeen < sessionPolicy.ReinforcementWindows.Day,
            _ => false,
        };
    }

    /// <summary>
    /// B8 (LM §17.1): a mastery row comes with exactly the dimension rows of its knowledge type. A missing or unexpected
    /// dimension is corrupt input, never synthesized as unmeasured. Checked for every unit before any candidate is built.
    /// </summary>
    private void EnsureCompleteDimensions(KnowledgeLearningState unit)
    {
        if (unit.Mastery is not { } mastery)
        {
            return;
        }

        var expected = masteryPolicy.DimensionsFor(unit.KnowledgeType);
        if (!mastery.Dimensions.Select(dimension => dimension.Dimension).ToHashSet().SetEquals(expected))
        {
            throw new ArgumentException(
                $"The mastery of knowledge unit {unit.KnowledgeUnitId} must have exactly the {unit.KnowledgeType} dimensions ({string.Join(", ", expected)}).",
                nameof(unit));
        }
    }

    private static CandidateRankKey RankKey(CandidateScore score, DateTime? nextReviewAtUtc, decimal curriculumRelevance, KnowledgeLearningState unit, ActivityType activityType) =>
        new(score.FinalScore, score.PriorityScore, nextReviewAtUtc, curriculumRelevance, unit.LessonItemSortOrder, unit.KnowledgeUnitId, activityType);
}
