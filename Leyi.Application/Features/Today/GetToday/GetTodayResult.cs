using Leyi.Domain.Learning;
using Leyi.Domain.Sessions;

namespace Leyi.Application.Features.Today.GetToday;

/// <summary>Today dashboard (API Contract §4). Counts are exact; recommendation and weakest dimension come from policies.</summary>
public sealed record GetTodayResult(
    DateOnly Date,
    int DailyGoalMinutes,
    int CompletedMinutes,
    int DueReviewCount,
    int DueLaterTodayCount,
    int NewKnowledgeAvailable,
    IReadOnlyList<int> DurationOptions,
    int RecommendedDurationMinutes,
    MasteryDimension? WeakestDimension,
    TodayResumeSession? ResumeSession,
    TodayCurrentLesson CurrentLesson);

/// <summary>The learner's open, not idle-expired session and how it would resume.</summary>
public sealed record TodayResumeSession(
    Guid SessionId,
    SessionStatus Status,
    SessionType SessionType,
    int? RequestedDurationMinutes,
    ResumeMode ResumeMode);

public sealed record TodayCurrentLesson(Guid LessonId, string Title);
