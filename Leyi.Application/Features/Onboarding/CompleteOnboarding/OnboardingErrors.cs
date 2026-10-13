using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Identity.Login;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>Errors of CompleteOnboarding (ErrorCodeCatalog: Onboarding / Profile, Authentication).</summary>
public static class OnboardingErrors
{
    public const string AlreadyCompletedCode = "ONBOARDING_ALREADY_COMPLETED";
    public const string LearningGoalInvalidCode = "LEARNING_GOAL_INVALID";
    public const string TargetHskInvalidCode = "TARGET_HSK_INVALID";
    public const string DailyStudyMinutesInvalidCode = "DAILY_STUDY_MINUTES_INVALID";
    public const string TimezoneInvalidCode = "TIMEZONE_INVALID";
    public const string LearningPlanNotFoundCode = "LEARNING_PLAN_NOT_FOUND";

    /// <summary>An active learning plan already exists (onboarding is complete).</summary>
    public static readonly Error AlreadyCompleted = new(AlreadyCompletedCode, "Initial onboarding is already completed.", ErrorType.Conflict);

    /// <summary>No active learning plan: onboarding is not completed (UpdateLearningGoal, GetToday).</summary>
    public static readonly Error LearningPlanNotFound = new(LearningPlanNotFoundCode, "Active learning plan missing.", ErrorType.NotFound);

    /// <summary>User missing or not ACTIVE (OB-G7).</summary>
    public static readonly Error AccountDisabled = new(LoginErrors.AccountDisabledCode, "The account is disabled.", ErrorType.Forbidden);
}
