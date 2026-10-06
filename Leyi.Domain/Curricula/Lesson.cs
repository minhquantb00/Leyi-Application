using Leyi.Domain.Common;

namespace Leyi.Domain.Curricula;

/// <summary>
/// Lesson inside a curriculum level (ERD §6.7). Completing a lesson is curriculum progress only;
/// it never changes knowledge mastery (Domain Model §6).
/// </summary>
public sealed class Lesson : Entity
{
    private readonly List<LessonKnowledgeUnit> knowledgeUnits = [];

    private Lesson()
    {
    }

    private Lesson(Guid id, Guid curriculumLevelId, string code, string title, short sortOrder, string? description, short? estimatedMinutes)
        : base(id)
    {
        CurriculumLevelId = curriculumLevelId;
        Code = code;
        Title = title;
        SortOrder = sortOrder;
        Description = description;
        EstimatedMinutes = estimatedMinutes;
        Status = PublicationStatus.Draft;
    }

    public Guid CurriculumLevelId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public short SortOrder { get; private set; }

    public short? EstimatedMinutes { get; private set; }

    public PublicationStatus Status { get; private set; }

    public IReadOnlyCollection<LessonKnowledgeUnit> KnowledgeUnits => knowledgeUnits;

    public static Lesson Create(
        Guid curriculumLevelId,
        string code,
        string title,
        short sortOrder,
        string? description = null,
        short? estimatedMinutes = null)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Sort order must not be negative.");
        }

        if (estimatedMinutes is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estimatedMinutes), estimatedMinutes, "Estimated minutes must be positive.");
        }

        return new Lesson(
            NewId(),
            Guard.NotEmpty(curriculumLevelId),
            Guard.NotBlank(code),
            Guard.NotBlank(title),
            sortOrder,
            Guard.NullIfBlank(description),
            estimatedMinutes);
    }

    /// <summary>Maps a knowledge unit into this lesson. A knowledge unit appears at most once per lesson.</summary>
    public LessonKnowledgeUnit AddKnowledgeUnit(Guid knowledgeUnitId, LessonKnowledgeRole role, short sortOrder, bool isRequired = true)
    {
        Guard.NotEmpty(knowledgeUnitId);

        if (knowledgeUnits.Any(link => link.KnowledgeUnitId == knowledgeUnitId))
        {
            throw new DomainException($"Knowledge unit '{knowledgeUnitId}' is already mapped to lesson '{Code}'.");
        }

        var link = LessonKnowledgeUnit.Create(Id, knowledgeUnitId, role, sortOrder, isRequired);
        knowledgeUnits.Add(link);
        return link;
    }

    public void Activate() => Status = PublicationStatus.Active;

    public void Archive() => Status = PublicationStatus.Archived;
}
