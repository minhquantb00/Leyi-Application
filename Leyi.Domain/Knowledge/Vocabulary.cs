using Leyi.Domain.Common;

namespace Leyi.Domain.Knowledge;

/// <summary>Vocabulary subtype of <see cref="KnowledgeUnit"/> (ERD §6.9). Key = KnowledgeUnitId.</summary>
public sealed class Vocabulary
{
    private readonly List<VocabularyExample> examples = [];
    private readonly List<VocabularyCollocation> collocations = [];

    private Vocabulary()
    {
    }

    public Guid KnowledgeUnitId { get; private set; }

    public string Hanzi { get; private set; } = string.Empty;

    public string Pinyin { get; private set; } = string.Empty;

    public string? PartOfSpeech { get; private set; }

    public string MeaningVi { get; private set; } = string.Empty;

    public string? MeaningEn { get; private set; }

    public string? HanViet { get; private set; }

    public byte? HskLevel { get; private set; }

    /// <summary>Canonical word audio (object storage key/URL; binary content lives in object storage).</summary>
    public string? AudioUrl { get; private set; }

    public IReadOnlyCollection<VocabularyExample> Examples => examples;

    public IReadOnlyCollection<VocabularyCollocation> Collocations => collocations;

    public VocabularyExample AddExample(string chineseText, string meaningVi, short sortOrder, string? pinyin = null, string? meaningEn = null, string? audioUrl = null, byte? difficultyLevel = null)
    {
        var example = VocabularyExample.Create(KnowledgeUnitId, chineseText, meaningVi, sortOrder, pinyin, meaningEn, audioUrl, difficultyLevel);
        examples.Add(example);
        return example;
    }

    public VocabularyCollocation AddCollocation(string text, string meaningVi, short sortOrder, string? pinyin = null)
    {
        var collocation = VocabularyCollocation.Create(KnowledgeUnitId, text, meaningVi, sortOrder, pinyin);
        collocations.Add(collocation);
        return collocation;
    }

    internal static Vocabulary Create(
        Guid knowledgeUnitId,
        string hanzi,
        string pinyin,
        string meaningVi,
        byte? hskLevel,
        string? partOfSpeech,
        string? meaningEn,
        string? hanViet,
        string? audioUrl) => new()
        {
            KnowledgeUnitId = knowledgeUnitId,
            Hanzi = Guard.NotBlank(hanzi),
            Pinyin = Guard.NotBlank(pinyin),
            MeaningVi = Guard.NotBlank(meaningVi),
            HskLevel = HskLevels.Validate(hskLevel, nameof(hskLevel)),
            PartOfSpeech = Guard.NullIfBlank(partOfSpeech),
            MeaningEn = Guard.NullIfBlank(meaningEn),
            HanViet = Guard.NullIfBlank(hanViet),
            AudioUrl = Guard.NullIfBlank(audioUrl),
        };
}
