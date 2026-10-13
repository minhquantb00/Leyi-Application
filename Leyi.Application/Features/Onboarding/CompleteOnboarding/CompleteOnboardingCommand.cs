using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// First-time onboarding (CompleteOnboarding.md, API §3). <paramref name="UserId"/> comes from the authenticated
/// principal, never from the request body. Enum values are the API wire values (e.g. <c>HSK4_PLUS</c>).
/// </summary>
public sealed record CompleteOnboardingCommand(
    Guid UserId,
    string? CurrentLevel,
    string? PrimaryGoal,
    int? TargetHskLevel,
    DateOnly? TargetDate,
    int? DailyStudyMinutes,
    OnboardingReminderInput? Reminder) : ICommand<Result<CompleteOnboardingResult>>;

/// <summary>Optional reminder of the onboarding request (OB-G5, OB-G6).</summary>
/// <param name="Enabled">Whether a reminder is wanted.</param>
/// <param name="LocalTime">Local time <c>HH:mm</c> (24-hour); required when enabled.</param>
/// <param name="Timezone">IANA timezone; required when enabled, and when present it becomes the user's timezone.</param>
public sealed record OnboardingReminderInput(bool? Enabled, string? LocalTime, string? Timezone);
