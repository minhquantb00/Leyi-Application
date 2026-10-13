using Leyi.Domain.Common;
using Leyi.Domain.Learning;
using Leyi.Domain.Mastery;
using Leyi.Domain.Sessions.Scoring;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>
/// RAW knowledge-unit-level pool membership (ASE §7, AS-G4, AS-G17). DueNow, Overdue and DueToday are mutually exclusive.
/// Membership is not a SelectionReason, not a ScoreContribution and not the reason the planner finally selects an activity:
/// those live in <see cref="AdaptiveCandidate.Score"/>. Membership can be broader than the numeric signals — e.g. a KU with
/// recent errors only on Listening is in <see cref="RecentError"/> for every candidate, but only the Listening candidates
/// score RecentError; a KU in <see cref="Reinforcement"/> with a stronger signal scores no ReinforcementNeed.
/// </summary>
[Flags]
public enum CandidatePools
{
    None = 0,

    /// <summary>Pool A: level ≥ 1, due, OverdueRatio below the threshold.</summary>
    DueNow = 1,

    /// <summary>Pool B: level ≥ 1, OverdueRatio at or above the threshold.</summary>
    Overdue = 2,

    /// <summary>Secondary: level ≥ 1, due later today.</summary>
    DueToday = 4,

    /// <summary>Pool C: KnowledgeState WEAK.</summary>
    Weak = 8,

    /// <summary>Pool D: RecentErrorCount above 0 on some dimension.</summary>
    RecentError = 16,

    /// <summary>Pool F: level 0 due, or level 1 first seen within a day and not due.</summary>
    Reinforcement = 32,
}

/// <summary>Deterministic ranking key of a candidate (AS-G12). Ordered by <see cref="CandidateRanking"/>.</summary>
public sealed record CandidateRankKey(
    decimal FinalScore,
    decimal PriorityScore,
    DateTime? NextReviewAtUtc,
    decimal CurriculumRelevance,
    int? LessonItemSortOrder,
    Guid KnowledgeUnitId,
    ActivityType ActivityType);

/// <summary>
/// One assessment candidate: a knowledge unit with one generatable assessment ActivityType (one candidate per
/// KnowledgeUnitId × ActivityType; all pool signals and reasons merged).
/// </summary>
/// <param name="Pools">Raw KU-level pool membership (see <see cref="CandidatePools"/>); numeric signals and reasons are in <paramref name="Score"/>.</param>
public sealed record AdaptiveCandidate(
    Guid KnowledgeUnitId,
    ActivityType ActivityType,
    MasteryDimension PrimaryDimension,
    CandidatePools Pools,
    ReviewDueStatus ReviewStatus,
    bool IsWeakSkillDimension,
    int ContentVersion,
    CandidateScore Score,
    CandidateRankKey RankKey)
{
    /// <summary>In Pool A or B (core review slot).</summary>
    public bool IsCoreReview => (Pools & (CandidatePools.DueNow | CandidatePools.Overdue)) != 0;

    /// <summary>
    /// The only membership is DueToday: the planner may use the candidate in its secondary pass only. Any other membership
    /// (DueToday + Reinforcement, + Weak, + RecentError…) makes it a main-pass candidate.
    /// </summary>
    public bool IsSecondaryOnly => Pools == CandidatePools.DueToday;
}

/// <summary>
/// A NEW vocabulary knowledge unit (Pool E) that can become the atomic bundle [NEW_VOCABULARY_EXPOSURE → Recall]
/// (AS-G7). Ranked by the score of its paired Recall (<see cref="RecallActivityType"/>); the exposure is not scored.
/// </summary>
public sealed record NewVocabularyCandidate(
    Guid KnowledgeUnitId,
    ActivityType RecallActivityType,
    bool IsWeakSkillDimension,
    int ContentVersion,
    CandidateScore RecallScore,
    CandidateRankKey RankKey);

/// <summary>
/// A learned vocabulary of an already-reached lesson of the current curriculum level, for the fallback ladder (AS-G10),
/// whether or not it is in a normal pool. <see cref="Candidate"/> carries the normal score and reasons.
/// </summary>
public sealed record FallbackCandidate
{
    /// <param name="candidate">The scored assessment candidate (its <see cref="AdaptiveCandidate.Pools"/> may be None).</param>
    /// <param name="knowledgeState">Mastery state (fallback step 2: STRONG / MASTERED).</param>
    /// <param name="lessonDistanceFromCurrent">Reached lessons back from the current one (current = 0, previous reached = 1, …; AS-G19).</param>
    public FallbackCandidate(AdaptiveCandidate candidate, KnowledgeState knowledgeState, int lessonDistanceFromCurrent)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        KnowledgeState = Guard.Defined(knowledgeState);
        LessonDistanceFromCurrent = lessonDistanceFromCurrent >= 0
            ? lessonDistanceFromCurrent
            : throw new ArgumentOutOfRangeException(nameof(lessonDistanceFromCurrent), lessonDistanceFromCurrent, "A reached lesson is never ahead of the current one.");
    }

    public AdaptiveCandidate Candidate { get; }

    public KnowledgeState KnowledgeState { get; }

    /// <summary>Fallback order of steps 2–3: nearest reached lesson first, then <see cref="CandidateRanking"/> (ASE §67, AS-G19).</summary>
    public int LessonDistanceFromCurrent { get; }

    /// <summary>In the current lesson (fallback step 1).</summary>
    public bool IsCurrentLesson => LessonDistanceFromCurrent == 0;
}

/// <summary>
/// Candidates for one session request, each list in the preliminary deterministic order (AS-G12). <see cref="Fallback"/> is
/// used only when the normal plan selects nothing.
/// </summary>
/// <param name="HasWeakSkillDimensions">
/// The learner has at least one ASE §54 weak skill dimension (from <see cref="CandidateBuildContext.WeakSkillDimensions"/>),
/// whether or not any candidate for it could be generated; the weak-skill target is 0 otherwise (AS-G5, AS-G18).
/// </param>
public sealed record CandidateSet(
    IReadOnlyList<AdaptiveCandidate> Assessments,
    IReadOnlyList<NewVocabularyCandidate> NewVocabulary,
    IReadOnlyList<FallbackCandidate> Fallback,
    bool HasWeakSkillDimensions);

/// <summary>
/// Tie-break order (AS-G12): FinalScore ↓, PriorityScore ↓, NextReviewAt ↑ (null last), CurriculumRelevance ↓,
/// LessonKnowledgeUnits.SortOrder ↑ (null last), KnowledgeUnitId ↑, ActivityType ↑.
/// </summary>
public sealed class CandidateRanking : IComparer<CandidateRankKey>
{
    public static CandidateRanking Instance { get; } = new();

    public int Compare(CandidateRankKey? x, CandidateRankKey? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        var result = y.FinalScore.CompareTo(x.FinalScore);
        if (result == 0)
        {
            result = y.PriorityScore.CompareTo(x.PriorityScore);
        }

        if (result == 0)
        {
            result = CompareNullLast(x.NextReviewAtUtc, y.NextReviewAtUtc);
        }

        if (result == 0)
        {
            result = y.CurriculumRelevance.CompareTo(x.CurriculumRelevance);
        }

        if (result == 0)
        {
            result = CompareNullLast(x.LessonItemSortOrder, y.LessonItemSortOrder);
        }

        if (result == 0)
        {
            result = x.KnowledgeUnitId.CompareTo(y.KnowledgeUnitId);
        }

        return result == 0 ? x.ActivityType.CompareTo(y.ActivityType) : result;
    }

    private static int CompareNullLast<T>(T? x, T? y)
        where T : struct, IComparable<T> =>
        (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => x.Value.CompareTo(y.Value),
        };
}
