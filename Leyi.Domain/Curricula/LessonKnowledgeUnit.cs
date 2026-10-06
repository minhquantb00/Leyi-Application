using Leyi.Domain.Common;

namespace Leyi.Domain.Curricula;

/// <summary>Lesson ↔ KnowledgeUnit mapping (ERD §6.13). Created through <see cref="Lesson.AddKnowledgeUnit"/>.</summary>
public sealed class LessonKnowledgeUnit : Entity
{
    private LessonKnowledgeUnit()
    {
    }

    private LessonKnowledgeUnit(Guid id, Guid lessonId, Guid knowledgeUnitId, LessonKnowledgeRole role, short sortOrder, bool isRequired)
        : base(id)
    {
        LessonId = lessonId;
        KnowledgeUnitId = knowledgeUnitId;
        Role = role;
        SortOrder = sortOrder;
        IsRequired = isRequired;
    }

    public Guid LessonId { get; private set; }

    public Guid KnowledgeUnitId { get; private set; }

    public LessonKnowledgeRole Role { get; private set; }

    public short SortOrder { get; private set; }

    /// <summary>Required for first-pass lesson completion.</summary>
    public bool IsRequired { get; private set; }

    internal static LessonKnowledgeUnit Create(Guid lessonId, Guid knowledgeUnitId, LessonKnowledgeRole role, short sortOrder, bool isRequired)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Sort order must not be negative.");
        }

        return new LessonKnowledgeUnit(NewId(), lessonId, knowledgeUnitId, Guard.Defined(role), sortOrder, isRequired);
    }
}
