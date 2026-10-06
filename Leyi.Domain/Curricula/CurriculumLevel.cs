using Leyi.Domain.Common;

namespace Leyi.Domain.Curricula;

/// <summary>
/// Ordered level inside a curriculum (ERD §6.6), e.g. FOUNDATION / HSK1…HSK6.
/// Code and SortOrder are unique within the curriculum (enforced by the database).
/// </summary>
public sealed class CurriculumLevel : Entity
{
    private CurriculumLevel()
    {
    }

    private CurriculumLevel(Guid id, Guid curriculumId, string code, string name, short sortOrder)
        : base(id)
    {
        CurriculumId = curriculumId;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
    }

    public Guid CurriculumId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public short SortOrder { get; private set; }

    public static CurriculumLevel Create(Guid curriculumId, string code, string name, short sortOrder) =>
        new(NewId(), Guard.NotEmpty(curriculumId), Guard.NotBlank(code), Guard.NotBlank(name), NotNegative(sortOrder));

    private static short NotNegative(short sortOrder) =>
        sortOrder < 0 ? throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Sort order must not be negative.") : sortOrder;
}
