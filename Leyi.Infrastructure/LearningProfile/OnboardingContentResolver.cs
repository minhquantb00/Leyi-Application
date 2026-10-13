using Leyi.Application.Features.Onboarding.CompleteOnboarding;
using Leyi.Domain.Curricula;
using Leyi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leyi.Infrastructure.LearningProfile;

/// <summary>
/// Read-only content lookup for onboarding (OB-G1, N4): exactly one ACTIVE curriculum; inside it the level with the
/// required code (unique per curriculum, <c>UX_CurriculumLevels_Curriculum_Code</c>; levels have no status in V1);
/// its first ACTIVE lesson by <c>SortOrder ASC, Id ASC</c>. Anything else is a content configuration error: logged with
/// the reason and thrown, so the learner gets a generic 500. Never falls back to another level.
/// </summary>
internal sealed partial class OnboardingContentResolver : IOnboardingContentResolver
{
    private readonly LeyiDbContext dbContext;
    private readonly ILogger<OnboardingContentResolver> logger;

    public OnboardingContentResolver(LeyiDbContext dbContext, ILogger<OnboardingContentResolver> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<OnboardingContentStart> ResolveAsync(string requiredLevelCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredLevelCode);

        // Two rows are enough to tell "none" from "more than one".
        var activeCurricula = await dbContext.Curriculums.AsNoTracking()
            .Where(curriculum => curriculum.Status == PublicationStatus.Active)
            .Select(curriculum => curriculum.Id)
            .Take(2)
            .ToListAsync(cancellationToken);
        if (activeCurricula.Count != 1)
        {
            throw ConfigurationError(activeCurricula.Count == 0
                ? "no ACTIVE curriculum exists"
                : "more than one ACTIVE curriculum exists");
        }

        var curriculumId = activeCurricula[0];
        var levelId = await dbContext.CurriculumLevels.AsNoTracking()
            .Where(level => level.CurriculumId == curriculumId && level.Code == requiredLevelCode)
            .Select(level => (Guid?)level.Id)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw ConfigurationError($"the ACTIVE curriculum {curriculumId} has no level {requiredLevelCode}");

        var lessonId = await dbContext.Lessons.AsNoTracking()
            .Where(lesson => lesson.CurriculumLevelId == levelId && lesson.Status == PublicationStatus.Active)
            .OrderBy(lesson => lesson.SortOrder)
            .ThenBy(lesson => lesson.Id)
            .Select(lesson => (Guid?)lesson.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ConfigurationError($"level {requiredLevelCode} ({levelId}) of the ACTIVE curriculum {curriculumId} has no ACTIVE lesson");

        return new OnboardingContentStart(curriculumId, levelId, lessonId);
    }

    private InvalidOperationException ConfigurationError(string reason)
    {
        LogContentConfigurationError(logger, reason);
        return new InvalidOperationException($"Onboarding content configuration error: {reason}.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Onboarding content configuration error: {Reason}")]
    private static partial void LogContentConfigurationError(ILogger logger, string reason);
}
