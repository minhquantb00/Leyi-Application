using System.Globalization;
using FluentValidation;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// Onboarding input rules (C8, OB-G5, OB-G6). Each rule carries the catalog code of its field; see
/// <see cref="LearningGoalRules.ToError"/> for how codes combine.
/// </summary>
internal sealed class CompleteOnboardingValidator : AbstractValidator<CompleteOnboardingCommand>
{
    /// <summary>Exact V1 reminder time format (24-hour).</summary>
    public const string LocalTimeFormat = "HH:mm";

    /// <summary>Column length of <c>Users.PreferredTimezone</c> / <c>UserReminders.Timezone</c>.</summary>
    public const int TimezoneMaxLength = 64;

    public CompleteOnboardingValidator()
    {
        RuleFor(command => command.CurrentLevel)
            .Must(value => OnboardingWireValues.TryParseSelfReportedLevel(value, out _))
            .WithMessage("Current level must be one of ZERO, BEGINNER, HSK1, HSK2, HSK3, HSK4_PLUS, UNSURE.")
            .WithErrorCode(Error.ValidationErrorCode)
            .OverridePropertyName("currentLevel");

        RuleFor(command => command.PrimaryGoal).MustBePrimaryGoal();

        RuleFor(command => command.TargetHskLevel)
            .MustBePresentForHskGoal()
            .When(command => LearningGoalRules.IsHskGoal(command.PrimaryGoal));

        RuleFor(command => command.TargetHskLevel)
            .MustBeHskLevel()
            .When(command => command.TargetHskLevel is not null);

        RuleFor(command => command.DailyStudyMinutes).MustBeDailyStudyMinutes();

        When(command => command.Reminder is not null, () =>
        {
            RuleFor(command => command.Reminder!.Enabled)
                .NotNull()
                .WithMessage("Reminder enabled flag is required.")
                .WithErrorCode(Error.ValidationErrorCode)
                .OverridePropertyName("reminder.enabled");

            RuleFor(command => command.Reminder!.LocalTime)
                .NotEmpty()
                .When(command => command.Reminder!.Enabled == true)
                .WithMessage("Reminder local time is required when the reminder is enabled.")
                .WithErrorCode(Error.ValidationErrorCode)
                .OverridePropertyName("reminder.localTime");

            RuleFor(command => command.Reminder!.LocalTime)
                .Must(value => TryParseLocalTime(value, out _))
                .When(command => !string.IsNullOrEmpty(command.Reminder!.LocalTime))
                .WithMessage($"Reminder local time must use the 24-hour format {LocalTimeFormat}.")
                .WithErrorCode(Error.ValidationErrorCode)
                .OverridePropertyName("reminder.localTime");

            RuleFor(command => command.Reminder!.Timezone)
                .NotEmpty()
                .When(command => command.Reminder!.Enabled == true)
                .WithMessage("Reminder timezone is required when the reminder is enabled.")
                .WithErrorCode(Error.ValidationErrorCode)
                .OverridePropertyName("reminder.timezone");

            RuleFor(command => command.Reminder!.Timezone)
                .Must(IsValidTimezone)
                .When(command => !string.IsNullOrWhiteSpace(command.Reminder!.Timezone))
                .WithMessage("Reminder timezone must be a valid IANA timezone.")
                .WithErrorCode(OnboardingErrors.TimezoneInvalidCode)
                .OverridePropertyName("reminder.timezone");
        });
    }

    public static bool TryParseLocalTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, LocalTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);

    /// <summary>An IANA id known to the platform (Windows ids such as "SE Asia Standard Time" are rejected).</summary>
    public static bool IsValidTimezone(string? value) =>
        value is { Length: > 0 and <= TimezoneMaxLength }
        && TimeZoneInfo.TryFindSystemTimeZoneById(value, out var timeZone)
        && timeZone.HasIanaId;
}
