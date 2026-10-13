using FluentValidation;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Domain.Knowledge;
using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding;

/// <summary>
/// Learning goal input rules shared by CompleteOnboarding and UpdateLearningGoal (C8, OB-G5), each with its catalog
/// code. The "HSK goal requires a target" rule depends on the goal, so validators apply it with <see cref="IsHskGoal"/>.
/// </summary>
internal static class LearningGoalRules
{
    public static bool IsHskGoal(string? primaryGoal) =>
        OnboardingWireValues.TryParsePrimaryGoal(primaryGoal, out var goal) && goal == PrimaryGoal.Hsk;

    public static IRuleBuilderOptions<T, string?> MustBePrimaryGoal<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(value => OnboardingWireValues.TryParsePrimaryGoal(value, out _))
            .WithMessage("Primary goal is not a valid learning goal.")
            .WithErrorCode(OnboardingErrors.LearningGoalInvalidCode)
            .OverridePropertyName("primaryGoal");

    public static IRuleBuilderOptions<T, int?> MustBePresentForHskGoal<T>(this IRuleBuilder<T, int?> rule) =>
        rule.NotNull()
            .WithMessage("Target HSK level is required for the HSK goal.")
            .WithErrorCode(OnboardingErrors.TargetHskInvalidCode)
            .OverridePropertyName("targetHskLevel");

    public static IRuleBuilderOptions<T, int?> MustBeHskLevel<T>(this IRuleBuilder<T, int?> rule) =>
        rule.Must(level => level is >= HskLevels.Min and <= HskLevels.Max)
            .WithMessage($"Target HSK level must be between {HskLevels.Min} and {HskLevels.Max}.")
            .WithErrorCode(OnboardingErrors.TargetHskInvalidCode)
            .OverridePropertyName("targetHskLevel");

    public static IRuleBuilderOptions<T, int?> MustBeDailyStudyMinutes<T>(this IRuleBuilder<T, int?> rule) =>
        rule.Must(minutes => minutes is not null && UserLearningGoal.IsValidDailyStudyMinutes(minutes.Value))
            .WithMessage($"Daily study minutes must be {UserLearningGoal.MinDailyStudyMinutes}–{UserLearningGoal.MaxDailyStudyMinutes} in steps of {UserLearningGoal.DailyStudyMinutesStep}.")
            .WithErrorCode(OnboardingErrors.DailyStudyMinutesInvalidCode)
            .OverridePropertyName("dailyStudyMinutes");

    /// <summary>
    /// One field-specific code when every failure shares it (e.g. only the daily minutes are wrong →
    /// <c>DAILY_STUDY_MINUTES_INVALID</c>); <c>VALIDATION_ERROR</c> when failures span several codes. Details always
    /// list every failing field.
    /// </summary>
    public static Error ToError(FluentValidation.Results.ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        var codes = validation.Errors.Select(failure => failure.ErrorCode).Distinct(StringComparer.Ordinal).ToList();
        IReadOnlyDictionary<string, string[]> details = validation.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray(), StringComparer.Ordinal);

        return codes is [var single] && single != Error.ValidationErrorCode
            ? new Error(single, "One or more validation errors occurred.", ErrorType.Validation) { Details = details }
            : Error.Validation(details);
    }
}
