using Leyi.Domain.Common;

namespace Leyi.Domain.Knowledge;

/// <summary>Example sentence of a vocabulary (ERD §6.11).</summary>
public sealed class VocabularyExample : Entity
{
    private VocabularyExample()
    {
    }

    private VocabularyExample(Guid id)
        : base(id)
    {
    }

    public Guid VocabularyKnowledgeUnitId { get; private set; }

    public string ChineseText { get; private set; } = string.Empty;

    public string? Pinyin { get; private set; }

    public string MeaningVi { get; private set; } = string.Empty;

    public string? MeaningEn { get; private set; }

    public string? AudioUrl { get; private set; }

    public byte? DifficultyLevel { get; private set; }

    public short SortOrder { get; private set; }

    internal static VocabularyExample Create(
        Guid vocabularyKnowledgeUnitId,
        string chineseText,
        string meaningVi,
        short sortOrder,
        string? pinyin,
        string? meaningEn,
        string? audioUrl,
        byte? difficultyLevel) => new(NewId())
        {
            VocabularyKnowledgeUnitId = vocabularyKnowledgeUnitId,
            ChineseText = Guard.NotBlank(chineseText),
            MeaningVi = Guard.NotBlank(meaningVi),
            SortOrder = sortOrder,
            Pinyin = Guard.NullIfBlank(pinyin),
            MeaningEn = Guard.NullIfBlank(meaningEn),
            AudioUrl = Guard.NullIfBlank(audioUrl),
            DifficultyLevel = difficultyLevel,
        };
}
