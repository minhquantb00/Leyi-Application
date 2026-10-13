using System.Data;
using Leyi.Application.Features.Today.GetToday;
using Leyi.Domain.Curricula;
using Leyi.Domain.Knowledge;
using Leyi.Domain.Learning;
using Leyi.Domain.LearningProfile;
using Leyi.Domain.Mastery;
using Leyi.Domain.Policies;
using Leyi.Domain.Sessions;
using Leyi.Infrastructure.Identity;
using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.Today;

/// <summary>
/// Today read model (GetToday.md): a pure read of no-tracking projections and SQL aggregates scoped to one user. Only the
/// learning profile (active goal + active plan) is read in a short SERIALIZABLE snapshot, because its invariants are
/// checked; the counts run at READ COMMITTED (small timing skew between them is accepted). Order of checks (TD-G4): account ACTIVE;
/// neither active plan nor active goal → not onboarded; then the plan/goal invariants of OB-G9, the current lesson and
/// the timezone must be consistent, otherwise the state is logged and reported as a server error (never repaired).
/// </summary>
internal sealed partial class TodayReadStore : ITodayReadStore
{
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<TodayReadStore> logger;

    public TodayReadStore(LeyiDbContext dbContext, ILogger<TodayReadStore> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<TodayReadOutcome> ReadAsync(TodayReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = request.UserId;

        var user = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new { item.Status, item.PreferredTimezone })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return TodayReadOutcome.Rejected(TodayReadStatus.AccountDisabled);
        }

        var (goal, plan) = await LoadLearningProfileSnapshotAsync(userId, cancellationToken);

        if (goal is null && plan is null)
        {
            return TodayReadOutcome.Rejected(TodayReadStatus.LearningPlanNotFound);
        }

        if (plan is null)
        {
            throw InconsistentState(userId, "an active learning goal exists without an active learning plan");
        }

        if (goal is null)
        {
            throw InconsistentState(userId, "an active learning plan exists without an active learning goal");
        }

        if (plan.GoalId != goal.Id)
        {
            throw InconsistentState(userId, "the active learning plan is not linked to the active learning goal");
        }

        if (plan.TargetDate != goal.TargetDate)
        {
            throw InconsistentState(userId, "the active learning plan target date differs from the active learning goal");
        }

        if (plan.CurrentLessonId is not { } currentLessonId || plan.CurrentLessonTitle is null)
        {
            throw InconsistentState(userId, "the active learning plan has no current lesson");
        }

        if (!LearnerLocalDateResolver.TryFindTimeZone(user.PreferredTimezone, out var timeZone))
        {
            throw InconsistentState(userId, "the preferred timezone is not a valid IANA timezone");
        }

        var nowUtc = request.NowUtc;
        var localDay = LearnerLocalDateResolver.Resolve(nowUtc, timeZone);
        var reviews = await CountReviewsAsync(userId, nowUtc, localDay.NextLocalDayStartUtc, cancellationToken);

        var meaningfulLearningSeconds = await dbContext.DailyLearningStats.AsNoTracking()
            .Where(stats => stats.UserId == userId && stats.LocalDate == localDay.LocalDate)
            .Select(stats => (int?)stats.MeaningfulLearningSeconds)
            .SingleOrDefaultAsync(cancellationToken) ?? 0;

        return TodayReadOutcome.Found(new TodaySnapshot(
            localDay.LocalDate,
            goal.DailyStudyMinutes,
            meaningfulLearningSeconds,
            reviews.DueNow,
            reviews.DueLaterToday,
            await CountNewKnowledgeAsync(userId, currentLessonId, cancellationToken),
            await CountMeaningfulSessionsAsync(userId, request.MeaningfulSessionMinEvaluatedActivities, nowUtc - request.SessionExpiryThreshold, cancellationToken),
            await AverageDimensionScoresAsync(userId, request.MinDimensionEvidenceCount, cancellationToken),
            await dbContext.LearningSessions.AsNoTracking()
                .SingleOrDefaultAsync(session => session.UserId == userId && SessionStatusExtensions.OpenStatuses.Contains(session.Status), cancellationToken),
            currentLessonId,
            plan.CurrentLessonTitle));
    }

    /// <summary>
    /// The active goal and the active plan (with its current lesson) as one consistent view, so the OB-G9 invariants are
    /// never judged on rows from two different commits (TD-G4). A short read-only SERIALIZABLE transaction: its range
    /// locks also cover "no row yet", so a concurrent CompleteOnboarding cannot insert goal and plan between the two
    /// reads. Both reads seek on the filtered active index by UserId (index hint + FORCESEEK), so the locks stay on this
    /// user's key in those indexes (or, with no row, the gap between its neighbouring keys) instead of every user's rows.
    /// Goals are read before plans, the order in which UpdateLearningGoal and CompleteOnboarding write them, so a writer
    /// of the same user simply waits for the reader. Nothing is written; the transaction is committed right after the reads.
    /// </summary>
    private Task<(ProfileGoal? Goal, ProfilePlan? Plan)> LoadLearningProfileSnapshotAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Database.CreateExecutionStrategy().ExecuteAsync(
            userId,
            async (_, id, ct) =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

                // Seek on the filtered active indexes (the predicates repeat their filters) so the key-range locks
                // cover only this user's key; a clustered scan would range-lock every user's goals and plans.
                var goal = await dbContext.UserLearningGoals
                    .FromSql($"SELECT * FROM [dbo].[UserLearningGoals] WITH (INDEX([UX_UserLearningGoals_Active]), FORCESEEK) WHERE [UserId] = {id} AND [IsActive] = 1")
                    .AsNoTracking()
                    .Select(item => new ProfileGoal(item.Id, item.DailyStudyMinutes, item.TargetDate))
                    .SingleOrDefaultAsync(ct);
                var plan = await dbContext.UserLearningPlans
                    .FromSql($"SELECT * FROM [dbo].[UserLearningPlans] WITH (INDEX([UX_UserLearningPlans_User_Active]), FORCESEEK) WHERE [UserId] = {id} AND [Status] = 'ACTIVE'")
                    .AsNoTracking()
                    .Select(item => new ProfilePlan(
                        item.GoalId,
                        item.TargetDate,
                        item.CurrentLessonId,
                        dbContext.Lessons.Where(lesson => lesson.Id == item.CurrentLessonId).Select(lesson => lesson.Title).FirstOrDefault()))
                    .SingleOrDefaultAsync(ct);

                await transaction.CommitAsync(ct);
                return (goal, plan);
            },
            verifySucceeded: null,
            cancellationToken);

    /// <summary>
    /// ReviewDebt (level ≥ 1, due at or before now) and DueToday (level ≥ 1, after now, before the next local day) in one
    /// aggregate over <c>IX_ReviewSchedules_User_NextReviewAt</c>. Level 0 is excluded from both.
    /// </summary>
    private async Task<(int DueNow, int DueLaterToday)> CountReviewsAsync(Guid userId, DateTime nowUtc, DateTime nextLocalDayStartUtc, CancellationToken cancellationToken)
    {
        // Levels are 0–7 (check constraint), so "not the initial level" is "level ≥ 1".
        var counts = await dbContext.ReviewSchedules.AsNoTracking()
            .Where(schedule => schedule.UserId == userId
                && schedule.ReviewLevel != ReviewLevel.Initial
                && schedule.NextReviewAt < nextLocalDayStartUtc)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                DueNow = group.Count(schedule => schedule.NextReviewAt <= nowUtc),
                DueLaterToday = group.Count(schedule => schedule.NextReviewAt > nowUtc),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return counts is null ? (0, 0) : (counts.DueNow, counts.DueLaterToday);
    }

    /// <summary>
    /// NEW = no mastery row (TD-G1): links of the current lesson to ACTIVE knowledge units the user has no mastery for.
    /// Inventory only; nothing about what a session will select.
    /// </summary>
    private Task<int> CountNewKnowledgeAsync(Guid userId, Guid lessonId, CancellationToken cancellationToken) =>
        dbContext.Set<LessonKnowledgeUnit>().AsNoTracking()
            .Where(link => link.LessonId == lessonId)
            .Where(link => dbContext.KnowledgeUnits.Any(unit => unit.Id == link.KnowledgeUnitId && unit.Status == KnowledgeUnitStatus.Active))
            .Where(link => !dbContext.UserKnowledgeMasteries.Any(mastery => mastery.UserId == userId && mastery.KnowledgeUnitId == link.KnowledgeUnitId))
            .CountAsync(cancellationToken);

    /// <summary>
    /// Meaningful sessions (ASE §25, TD-G2): enough evaluated activities and ended — persisted COMPLETED / ABANDONED /
    /// EXPIRED, or still open but already expired by SessionExpiryPolicy (idle reference <c>LastActivityAt ?? GeneratedAt</c>
    /// older than the expiry threshold; exactly the threshold is not expired). The count therefore does not depend on
    /// whether a command or the cleanup job has materialized EXPIRED yet. Nothing is changed.
    /// </summary>
    private Task<int> CountMeaningfulSessionsAsync(Guid userId, int minEvaluatedActivities, DateTime expiredIfIdleBeforeUtc, CancellationToken cancellationToken) =>
        dbContext.LearningSessions.AsNoTracking()
            .CountAsync(
                session => session.UserId == userId
                    && session.EvaluatedActivityCount >= minEvaluatedActivities
                    && (SessionStatusExtensions.EndedStatuses.Contains(session.Status)
                        || (SessionStatusExtensions.OpenStatuses.Contains(session.Status)
                            && (session.LastActivityAt ?? session.GeneratedAt) < expiredIfIdleBeforeUtc)),
                cancellationToken);

    /// <summary>User-wide plain mean score per dimension over dimension masteries with enough evidence (TD-G3).</summary>
    private async Task<IReadOnlyCollection<DimensionAverage>> AverageDimensionScoresAsync(Guid userId, int minEvidenceCount, CancellationToken cancellationToken)
    {
        var averages = await (
                from mastery in dbContext.UserKnowledgeMasteries.AsNoTracking()
                join dimension in dbContext.Set<UserKnowledgeDimensionMastery>().AsNoTracking()
                    on mastery.Id equals dimension.UserKnowledgeMasteryId
                where mastery.UserId == userId && dimension.EvidenceCount >= minEvidenceCount
                group dimension.Score by dimension.Dimension into scores
                select new { Dimension = scores.Key, AverageScore = scores.Average() })
            .ToListAsync(cancellationToken);

        return averages.Select(average => new DimensionAverage(average.Dimension, average.AverageScore)).ToList();
    }

    private InvalidOperationException InconsistentState(Guid userId, string reason)
    {
        LogInconsistentState(logger, userId, reason);
        return new InvalidOperationException($"Inconsistent learning profile: {reason}.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Inconsistent learning profile for {UserId} on Today: {Reason}")]
    private static partial void LogInconsistentState(ILogger logger, Guid userId, string reason);

    private sealed record ProfileGoal(Guid Id, short DailyStudyMinutes, DateOnly? TargetDate);

    private sealed record ProfilePlan(Guid? GoalId, DateOnly? TargetDate, Guid? CurrentLessonId, string? CurrentLessonTitle);
}
