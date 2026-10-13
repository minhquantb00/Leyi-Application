using Leyi.Domain.Policies;
using Leyi.Domain.Sessions;

namespace Leyi.Application.Features.Today.GetToday;

/// <summary>
/// Read model of Today (pure read, scoped to one user). Checks the account and the persisted plan/goal/lesson state
/// (TD-G4): a missing or non-ACTIVE user and a not onboarded learner are outcomes; inconsistent persisted state (goal
/// without plan, plan without goal, GoalId/TargetDate mismatch, no current lesson, invalid timezone) is a server error
/// (an exception, logged), never repaired. Writes nothing.
/// </summary>
public interface ITodayReadStore
{
    Task<TodayReadOutcome> ReadAsync(TodayReadRequest request, CancellationToken cancellationToken);
}

/// <param name="UserId">Authenticated user.</param>
/// <param name="NowUtc">Instant of the request (one value for every boundary).</param>
/// <param name="MinDimensionEvidenceCount">Evidence a dimension mastery needs to be a skill-profile sample.</param>
/// <param name="MeaningfulSessionMinEvaluatedActivities">Evaluated activities that make an ended session meaningful (ASE §25).</param>
/// <param name="SessionExpiryThreshold">Idle time after which an open session is expired by SessionExpiryPolicy (counts as ended).</param>
public sealed record TodayReadRequest(
    Guid UserId,
    DateTime NowUtc,
    int MinDimensionEvidenceCount,
    int MeaningfulSessionMinEvaluatedActivities,
    TimeSpan SessionExpiryThreshold);

public enum TodayReadStatus
{
    Found,

    /// <summary>User missing or not ACTIVE.</summary>
    AccountDisabled,

    /// <summary>No active plan and no active goal: onboarding is not completed.</summary>
    LearningPlanNotFound,
}

public sealed record TodayReadOutcome(TodayReadStatus Status, TodaySnapshot? Snapshot)
{
    public static TodayReadOutcome Found(TodaySnapshot snapshot) => new(TodayReadStatus.Found, snapshot ?? throw new ArgumentNullException(nameof(snapshot)));

    public static TodayReadOutcome Rejected(TodayReadStatus status) =>
        status == TodayReadStatus.Found
            ? throw new ArgumentOutOfRangeException(nameof(status), status, "A found read carries its snapshot.")
            : new(status, null);
}

/// <summary>Persisted facts of the learner's day; the handler applies the policies.</summary>
/// <param name="LocalDate">Learner local date (<c>Users.PreferredTimezone</c>).</param>
/// <param name="DailyGoalMinutes">Daily study minutes of the active goal.</param>
/// <param name="MeaningfulLearningSeconds">DailyLearningStats of the local date; 0 without a row.</param>
/// <param name="DueReviewCount">ReviewDebt: level ≥ 1 and due now.</param>
/// <param name="DueLaterTodayCount">DueToday: level ≥ 1, due after now and before the next local day.</param>
/// <param name="NewKnowledgeAvailable">Active knowledge units of the current lesson without a mastery row.</param>
/// <param name="MeaningfulSessionCount">Ended (persisted or expired by idle time) sessions with enough evaluated activities.</param>
/// <param name="DimensionAverages">User-wide mean score per dimension over eligible samples.</param>
/// <param name="OpenSession">The open session, if any (read-only, possibly idle-expired).</param>
/// <param name="CurrentLessonId">Current lesson of the active plan.</param>
/// <param name="CurrentLessonTitle">Its title.</param>
public sealed record TodaySnapshot(
    DateOnly LocalDate,
    int DailyGoalMinutes,
    int MeaningfulLearningSeconds,
    int DueReviewCount,
    int DueLaterTodayCount,
    int NewKnowledgeAvailable,
    int MeaningfulSessionCount,
    IReadOnlyCollection<DimensionAverage> DimensionAverages,
    LearningSession? OpenSession,
    Guid CurrentLessonId,
    string CurrentLessonTitle);
