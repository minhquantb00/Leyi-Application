using FluentValidation;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// Creates the learner's goal, learning plan and optional reminder (CompleteOnboarding.md). The starting point comes
/// from <see cref="OnboardingStartPolicy"/> (N4) and the single ACTIVE curriculum (OB-G1). The self-reported level is
/// stored on the goal only: no mastery, evidence, review schedule, lesson progress or session is created (OB-G10),
/// and no integration event is published (OB-G8).
/// </summary>
internal sealed class CompleteOnboardingHandler : ICommandHandler<CompleteOnboardingCommand, Result<CompleteOnboardingResult>>
{
    private readonly IValidator<CompleteOnboardingCommand> validator;
    private readonly IOnboardingContentResolver contentResolver;
    private readonly IOnboardingStore onboardingStore;
    private readonly TimeProvider timeProvider;

    public CompleteOnboardingHandler(
        IValidator<CompleteOnboardingCommand> validator,
        IOnboardingContentResolver contentResolver,
        IOnboardingStore onboardingStore,
        TimeProvider timeProvider)
    {
        this.validator = validator;
        this.contentResolver = contentResolver;
        this.onboardingStore = onboardingStore;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<CompleteOnboardingResult>> HandleAsync(CompleteOnboardingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException("The authenticated user id is required.", nameof(command));
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return LearningGoalRules.ToError(validation);
        }

        // Existing state decides the error first (403 → 409 → inconsistent state); content is resolved only for an
        // eligible learner, so a content configuration error never hides those outcomes.
        switch (await onboardingStore.CheckEligibilityAsync(command.UserId, cancellationToken))
        {
            case OnboardingEligibility.Eligible:
                break;
            case OnboardingEligibility.AccountDisabled:
                return OnboardingErrors.AccountDisabled;
            case OnboardingEligibility.AlreadyCompleted:
                return OnboardingErrors.AlreadyCompleted;
            case OnboardingEligibility.InconsistentState:
                throw new InvalidOperationException("Onboarding found an active learning goal without an active learning plan.");
            case var eligibility:
                throw new InvalidOperationException($"Unknown onboarding eligibility {eligibility}.");
        }

        OnboardingWireValues.TryParseSelfReportedLevel(command.CurrentLevel, out var selfReportedLevel);
        OnboardingWireValues.TryParsePrimaryGoal(command.PrimaryGoal, out var primaryGoal);

        var start = OnboardingStartPolicy.For(selfReportedLevel);
        var content = await contentResolver.ResolveAsync(start.RequiredLevelCode, cancellationToken);
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var goal = UserLearningGoal.Create(
            command.UserId,
            primaryGoal,
            selfReportedLevel,
            (short)command.DailyStudyMinutes!.Value,
            command.TargetHskLevel is { } targetHskLevel ? (byte)targetHskLevel : null,
            command.TargetDate);

        // The plan's id is fixed here, once per logical request: the store uses it to recognize its own commit.
        var plan = UserLearningPlan.Start(
            command.UserId,
            content.CurriculumId,
            start.PlanType,
            start.PlacementStatus,
            nowUtc,
            goal.Id,
            content.LevelId,
            content.LessonId,
            command.TargetDate);

        var registration = new OnboardingRegistration(
            command.UserId,
            goal,
            plan,
            CreateReminder(command.UserId, command.Reminder),
            NormalizedTimezone(command.Reminder),
            nowUtc);

        return await onboardingStore.SaveAsync(registration, cancellationToken) switch
        {
            OnboardingSaveOutcome.Saved => new CompleteOnboardingResult(plan.Id, start.PlanType, start.RequiredLevelCode, content.LessonId, start.PlacementStatus),
            OnboardingSaveOutcome.AccountDisabled => OnboardingErrors.AccountDisabled,
            OnboardingSaveOutcome.AlreadyCompleted => OnboardingErrors.AlreadyCompleted,
            var outcome => throw new InvalidOperationException($"Unknown onboarding save outcome {outcome}."),
        };
    }

    /// <summary>Only an enabled reminder is stored (OB-G5): every day of the week at the given local time.</summary>
    private static UserReminder? CreateReminder(Guid userId, OnboardingReminderInput? reminder)
    {
        if (reminder is not { Enabled: true })
        {
            return null;
        }

        CompleteOnboardingValidator.TryParseLocalTime(reminder.LocalTime, out var localTime);
        return UserReminder.Create(userId, enabled: true, localTime, reminder.Timezone!);
    }

    /// <summary>A supplied timezone becomes the user's timezone whether or not the reminder is enabled (OB-G6).</summary>
    private static string? NormalizedTimezone(OnboardingReminderInput? reminder) =>
        string.IsNullOrWhiteSpace(reminder?.Timezone) ? null : reminder.Timezone;
}
