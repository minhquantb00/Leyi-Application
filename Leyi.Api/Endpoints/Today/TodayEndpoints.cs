using System.Security.Claims;
using Leyi.Api.Authentication;
using Leyi.Api.ErrorHandling;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Today.GetToday;
using Leyi.Domain.Learning;
using Leyi.Domain.Sessions;

namespace Leyi.Api.Endpoints.Today;

/// <summary>Today dashboard (API Contract §4). Authenticated; the user id comes from the access token "sub" claim.</summary>
internal static class TodayEndpoints
{
    public static IEndpointRouteBuilder MapTodayEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/today", GetTodayAsync)
            .WithTags("Today")
            .RequireAuthorization()
            .WithName("GetToday")
            .Produces<TodayResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetTodayAsync(
        ClaimsPrincipal user,
        IQueryHandler<GetTodayQuery, Result<GetTodayResult>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTodayQuery(user.GetRequiredUserId()), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        var today = result.Value;
        return TypedResults.Ok(new TodayResponse(
            today.Date,
            today.DailyGoalMinutes,
            today.CompletedMinutes,
            today.DueReviewCount,
            today.DueLaterTodayCount,
            today.NewKnowledgeAvailable,
            today.DurationOptions,
            today.RecommendedDurationMinutes,
            today.WeakestDimension is { } dimension ? ToWire(dimension) : null,
            today.ResumeSession is { } session
                ? new TodayResumeSessionResponse(session.SessionId, ToWire(session.Status), ToWire(session.SessionType), session.RequestedDurationMinutes, ToWire(session.ResumeMode))
                : null,
            new TodayCurrentLessonResponse(today.CurrentLesson.LessonId, today.CurrentLesson.Title)));
    }

    private static string ToWire(MasteryDimension dimension) => dimension switch
    {
        MasteryDimension.Recognition => "RECOGNITION",
        MasteryDimension.Recall => "RECALL",
        MasteryDimension.Listening => "LISTENING",
        MasteryDimension.Usage => "USAGE",
        _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, "Unknown mastery dimension."),
    };

    private static string ToWire(SessionStatus status) => status switch
    {
        SessionStatus.Generated => "GENERATED",
        SessionStatus.InProgress => "IN_PROGRESS",
        SessionStatus.Paused => "PAUSED",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Only an open session can be resumed."),
    };

    private static string ToWire(SessionType sessionType) => sessionType switch
    {
        SessionType.AdaptiveQuick => "ADAPTIVE_QUICK",
        SessionType.Lesson => "LESSON",
        SessionType.Practice => "PRACTICE",
        SessionType.Placement => "PLACEMENT",
        _ => throw new ArgumentOutOfRangeException(nameof(sessionType), sessionType, "Unknown session type."),
    };

    private static string ToWire(ResumeMode resumeMode) => resumeMode switch
    {
        ResumeMode.Continue => "CONTINUE",
        ResumeMode.RegenerateRemaining => "REGENERATE_REMAINING",
        _ => throw new ArgumentOutOfRangeException(nameof(resumeMode), resumeMode, "Unknown resume mode."),
    };
}

/// <summary>GET /today 200 response (API Contract §4). Nullable: <c>weakestDimension</c>, <c>resumeSession</c>.</summary>
public sealed record TodayResponse(
    DateOnly Date,
    int DailyGoalMinutes,
    int CompletedMinutes,
    int DueReviewCount,
    int DueLaterTodayCount,
    int NewKnowledgeAvailable,
    IReadOnlyList<int> DurationOptions,
    int RecommendedDurationMinutes,
    string? WeakestDimension,
    TodayResumeSessionResponse? ResumeSession,
    TodayCurrentLessonResponse CurrentLesson);

/// <summary>Open session to resume; <c>requestedDurationMinutes</c> is null when the session has no requested duration.</summary>
public sealed record TodayResumeSessionResponse(
    Guid SessionId,
    string Status,
    string SessionType,
    int? RequestedDurationMinutes,
    string ResumeMode);

public sealed record TodayCurrentLessonResponse(Guid LessonId, string Title);
