using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// The new learning plan. <paramref name="CurrentLevelCode"/> is the <c>CurriculumLevel.Code</c> chosen by
/// <see cref="OnboardingStartPolicy"/>, not the self-reported level.
/// </summary>
public sealed record CompleteOnboardingResult(
    Guid LearningPlanId,
    PlanType PlanType,
    string CurrentLevelCode,
    Guid CurrentLessonId,
    PlacementStatus PlacementStatus);
