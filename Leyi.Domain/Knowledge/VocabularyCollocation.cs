using Leyi.Domain.Common;

namespace Leyi.Domain.Knowledge;

/// <summary>Collocation of a vocabulary (ERD §6.12).</summary>
public sealed class VocabularyCollocation : Entity
{
    private VocabularyCollocation()
    {
    }

    private VocabularyCollocation(Guid id)
        : base(id)
    {
    }

    public Guid VocabularyKnowledgeUnitId { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public string? Pinyin { get; private set; }

    public string MeaningVi { get; private set; } = string.Empty;

    public short SortOrder { get; private set; }

    internal static VocabularyCollocation Create(Guid vocabularyKnowledgeUnitId, string text, string meaningVi, short sortOrder, string? pinyin) =>
        new(NewId())
        {
            VocabularyKnowledgeUnitId = vocabularyKnowledgeUnitId,
            Text = Guard.NotBlank(text),
            MeaningVi = Guard.NotBlank(meaningVi),
            SortOrder = sortOrder,
            Pinyin = Guard.NullIfBlank(pinyin),
        };
}
