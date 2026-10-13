using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// Persists a completed onboarding atomically: goal, plan, optional reminder and the user's timezone, after checking —
/// inside the same transaction — that the user is ACTIVE and has no active learning plan yet. Concurrent onboarding of
/// one user must end with at most one active goal and one active plan.
/// </summary>
public interface IOnboardingStore
{
    /// <summary>
    /// Unlocked read of the state that already exists before this request, so that it decides the error precedence
    /// (403, then 409, then inconsistent state) before any content is resolved. Not a concurrency guard:
    /// <see cref="SaveAsync"/> checks the same rules again under the user's lock and decides the final outcome.
    /// </summary>
    Task<OnboardingEligibility> CheckEligibilityAsync(Guid userId, CancellationToken cancellationToken);

    Task<OnboardingSaveOutcome> SaveAsync(OnboardingRegistration registration, CancellationToken cancellationToken);
}

public enum OnboardingEligibility
{
    Eligible,

    /// <summary>User missing or not ACTIVE.</summary>
    AccountDisabled,

    /// <summary>An active learning plan exists: onboarding is complete.</summary>
    AlreadyCompleted,

    /// <summary>An active learning goal exists without an active learning plan (logged by the store).</summary>
    InconsistentState,
}

/// <param name="UserId">Authenticated user.</param>
/// <param name="Goal">New active goal.</param>
/// <param name="Plan">New active plan (its id identifies this onboarding operation).</param>
/// <param name="Reminder">New reminder, only for an enabled reminder.</param>
/// <param name="PreferredTimezone">New <c>Users.PreferredTimezone</c>, or null to keep the current one.</param>
/// <param name="NowUtc">Time of the operation.</param>
public sealed record OnboardingRegistration(
    Guid UserId,
    UserLearningGoal Goal,
    UserLearningPlan Plan,
    UserReminder? Reminder,
    string? PreferredTimezone,
    DateTime NowUtc);

public enum OnboardingSaveOutcome
{
    Saved,

    /// <summary>User missing or not ACTIVE. Nothing was written.</summary>
    AccountDisabled,

    /// <summary>An active learning plan already exists. Nothing was written.</summary>
    AlreadyCompleted,
}
