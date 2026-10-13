using System.Security.Claims;
using Leyi.Api.Authentication;
using Leyi.Api.ErrorHandling;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Application.Features.Onboarding.UpdateLearningGoal;
using Leyi.Domain.LearningProfile;

namespace Leyi.Api.Endpoints.Onboarding;

/// <summary>Onboarding and learning goal endpoints (API Contract §3). Authenticated; the user id comes from the access token "sub" claim.</summary>
internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/onboarding", CompleteOnboardingAsync)
            .WithTags("Onboarding")
            .RequireAuthorization()
            .WithName("CompleteOnboarding")
            .Produces<CompleteOnboardingResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPut("/api/v1/learning-goal", UpdateLearningGoalAsync)
            .WithTags("Onboarding")
            .RequireAuthorization()
            .WithName("UpdateLearningGoal")
            .Produces<LearningGoalResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> UpdateLearningGoalAsync(
        UpdateLearningGoalRequest request,
        ClaimsPrincipal user,
        ICommandHandler<UpdateLearningGoalCommand, Result<UpdateLearningGoalResult>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateLearningGoalCommand(user.GetRequiredUserId(), request.PrimaryGoal, request.TargetHskLevel, request.TargetDate, request.DailyStudyMinutes),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        // Same response whether a new goal version was created or the request was a no-op.
        var goal = result.Value;
        return TypedResults.Ok(new LearningGoalResponse(
            OnboardingWireValues.ToWire(goal.PrimaryGoal),
            OnboardingWireValues.ToWire(goal.SelfReportedLevel),
            goal.TargetHskLevel,
            goal.TargetDate,
            goal.DailyStudyMinutes));
    }

    private static async Task<IResult> CompleteOnboardingAsync(
        CompleteOnboardingRequest request,
        ClaimsPrincipal user,
        ICommandHandler<CompleteOnboardingCommand, Result<CompleteOnboardingResult>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CompleteOnboardingCommand(
            user.GetRequiredUserId(),
            request.CurrentLevel,
            request.PrimaryGoal,
            request.TargetHskLevel,
            request.TargetDate,
            request.DailyStudyMinutes,
            request.Reminder is { } reminder ? new OnboardingReminderInput(reminder.Enabled, reminder.LocalTime, reminder.Timezone) : null);

        var result = await handler.HandleAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error.ToProblem();
        }

        var completed = result.Value;
        return TypedResults.Ok(new CompleteOnboardingResponse(
            completed.LearningPlanId,
            ToWire(completed.PlanType),
            completed.CurrentLevelCode,
            completed.CurrentLessonId,
            ToWire(completed.PlacementStatus)));
    }

    private static string ToWire(PlanType planType) => planType switch
    {
        PlanType.Foundation => "FOUNDATION",
        PlanType.Standard => "STANDARD",
        PlanType.Adaptive => "ADAPTIVE",
        PlanType.HskPreparation => "HSK_PREPARATION",
        _ => throw new ArgumentOutOfRangeException(nameof(planType), planType, "Unknown plan type."),
    };

    private static string ToWire(PlacementStatus placementStatus) => placementStatus switch
    {
        PlacementStatus.NotApplicable => "NOT_APPLICABLE",
        PlacementStatus.Recommended => "RECOMMENDED",
        PlacementStatus.Skipped => "SKIPPED",
        PlacementStatus.Completed => "COMPLETED",
        _ => throw new ArgumentOutOfRangeException(nameof(placementStatus), placementStatus, "Unknown placement status."),
    };
}

/// <summary>POST /onboarding request (API Contract §3). Explicit contract; no user id, no fields beyond the contract.</summary>
public sealed record CompleteOnboardingRequest(
    string? CurrentLevel,
    string? PrimaryGoal,
    int? TargetHskLevel,
    DateOnly? TargetDate,
    int? DailyStudyMinutes,
    OnboardingReminderRequest? Reminder);

/// <summary>Optional reminder: <c>localTime</c> is <c>HH:mm</c> (24-hour), <c>timezone</c> an IANA id.</summary>
public sealed record OnboardingReminderRequest(bool? Enabled, string? LocalTime, string? Timezone);

/// <summary>
/// POST /onboarding 200 response (API Contract §3). <c>currentLevel</c> is the curriculum level code the plan starts
/// at (FOUNDATION, HSK1…HSK4), not the self-reported level.
/// </summary>
public sealed record CompleteOnboardingResponse(
    Guid LearningPlanId,
    string PlanType,
    string CurrentLevel,
    Guid CurrentLessonId,
    string PlacementStatus);

/// <summary>PUT /learning-goal request (API Contract §3, OB-G9): full desired state of the editable goal fields.</summary>
public sealed record UpdateLearningGoalRequest(
    string? PrimaryGoal,
    int? TargetHskLevel,
    DateOnly? TargetDate,
    int? DailyStudyMinutes);

/// <summary>PUT /learning-goal 200 response: the active goal. <c>selfReportedLevel</c> is read-only; no ids are exposed.</summary>
public sealed record LearningGoalResponse(
    string PrimaryGoal,
    string SelfReportedLevel,
    byte? TargetHskLevel,
    DateOnly? TargetDate,
    short DailyStudyMinutes);
