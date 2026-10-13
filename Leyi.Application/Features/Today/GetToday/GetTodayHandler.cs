using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Domain.Policies;

namespace Leyi.Application.Features.Today.GetToday;

/// <summary>
/// Today dashboard (GetToday.md, TD-G1…TD-G7). Pure read: never materializes session expiry, creates stats rows,
/// runs the Session Engine or publishes anything. Persisted facts come from <see cref="ITodayReadStore"/>; this handler
/// applies <see cref="RecommendedDurationPolicy"/>, <see cref="WeakestDimensionPolicy"/> and the session timing rules.
/// </summary>
internal sealed class GetTodayHandler : IQueryHandler<GetTodayQuery, Result<GetTodayResult>>
{
    private const int SecondsPerMinute = 60;

    private static readonly SessionPolicy SessionPolicy = SessionPolicy.V1;
    private static readonly RecommendedDurationPolicy RecommendedDurationPolicy = RecommendedDurationPolicy.V1;
    private static readonly WeakestDimensionPolicy WeakestDimensionPolicy = WeakestDimensionPolicy.V1;

    private readonly ITodayReadStore todayReadStore;
    private readonly TimeProvider timeProvider;

    public GetTodayHandler(ITodayReadStore todayReadStore, TimeProvider timeProvider)
    {
        this.todayReadStore = todayReadStore;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<GetTodayResult>> HandleAsync(GetTodayQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("The authenticated user id is required.", nameof(query));
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var outcome = await todayReadStore.ReadAsync(
            new TodayReadRequest(
                query.UserId,
                nowUtc,
                WeakestDimensionPolicy.MinEvidenceCount,
                SessionPolicy.ColdStart.MeaningfulSessionMinEvaluatedActivities,
                SessionPolicy.TimingRules.ExpiryThreshold),
            cancellationToken);

        return outcome.Status switch
        {
            TodayReadStatus.Found when outcome.Snapshot is { } snapshot => ToResult(snapshot, nowUtc),
            TodayReadStatus.AccountDisabled => OnboardingErrors.AccountDisabled,
            TodayReadStatus.LearningPlanNotFound => OnboardingErrors.LearningPlanNotFound,
            _ => throw new InvalidOperationException($"Unexpected Today read outcome {outcome.Status}."),
        };
    }

    private static GetTodayResult ToResult(TodaySnapshot snapshot, DateTime nowUtc)
    {
        var isBootstrap = SessionPolicy.ColdStart.IsBootstrap(snapshot.MeaningfulSessionCount);
        var completedMinutes = snapshot.MeaningfulLearningSeconds / SecondsPerMinute;

        return new GetTodayResult(
            snapshot.LocalDate,
            snapshot.DailyGoalMinutes,
            completedMinutes,
            snapshot.DueReviewCount,
            snapshot.DueLaterTodayCount,
            snapshot.NewKnowledgeAvailable,
            SessionPolicy.AllowedDurationsMinutes,
            RecommendedDurationPolicy.Recommend(isBootstrap, snapshot.DailyGoalMinutes, completedMinutes, snapshot.DueReviewCount),
            WeakestDimensionPolicy.Select(isBootstrap, snapshot.DimensionAverages),
            ToResumeSession(snapshot, nowUtc),
            new TodayCurrentLesson(snapshot.CurrentLessonId, snapshot.CurrentLessonTitle));
    }

    /// <summary>An idle-expired session is hidden but left as is (expiry is materialized by commands or the cleanup job).</summary>
    private static TodayResumeSession? ToResumeSession(TodaySnapshot snapshot, DateTime nowUtc)
    {
        var rules = SessionPolicy.TimingRules;
        if (snapshot.OpenSession is not { } session || session.IsIdleExpired(nowUtc, rules))
        {
            return null;
        }

        return new TodayResumeSession(
            session.Id,
            session.Status,
            session.SessionType,
            session.RequestedDurationSeconds / SecondsPerMinute,
            session.GetResumeMode(nowUtc, rules));
    }
}
