namespace Leyi.Application.Features.Onboarding.CompleteOnboarding;

/// <summary>
/// Finds the curriculum starting point for a required level code (OB-G1, N4): the single ACTIVE curriculum, the level
/// with that code inside it, and its first ACTIVE lesson (<c>SortOrder</c>, then <c>Id</c>). Missing or ambiguous
/// content is a server configuration error (an exception, surfaced as 500), never a learner-facing outcome.
/// </summary>
public interface IOnboardingContentResolver
{
    Task<OnboardingContentStart> ResolveAsync(string requiredLevelCode, CancellationToken cancellationToken);
}

public sealed record OnboardingContentStart(Guid CurriculumId, Guid LevelId, Guid LessonId);
