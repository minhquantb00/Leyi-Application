using Leyi.Domain.Common;

namespace Leyi.Domain.Knowledge;

/// <summary>
/// Abstract knowledge unit (ERD §6.8) with exactly one table-per-type subtype matching <see cref="Type"/>:
/// <see cref="Knowledge.Vocabulary"/> or <see cref="Knowledge.GrammarPattern"/>.
/// </summary>
public sealed class KnowledgeUnit : Entity
{
    private KnowledgeUnit()
    {
    }

    private KnowledgeUnit(Guid id, KnowledgeType type, string code)
        : base(id)
    {
        Type = type;
        Code = code;
        Status = KnowledgeUnitStatus.Active;
        ContentVersion = 1;
    }

    public KnowledgeType Type { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public KnowledgeUnitStatus Status { get; private set; }

    /// <summary>Version of the learning content; snapshotted by session activities (C3).</summary>
    public int ContentVersion { get; private set; }

    public Vocabulary? Vocabulary { get; private set; }

    public GrammarPattern? GrammarPattern { get; private set; }

    public static KnowledgeUnit CreateVocabulary(
        string code,
        string hanzi,
        string pinyin,
        string meaningVi,
        byte? hskLevel = null,
        string? partOfSpeech = null,
        string? meaningEn = null,
        string? hanViet = null,
        string? audioUrl = null)
    {
        var unit = new KnowledgeUnit(NewId(), KnowledgeType.Vocabulary, Guard.NotBlank(code));
        unit.Vocabulary = Vocabulary.Create(unit.Id, hanzi, pinyin, meaningVi, hskLevel, partOfSpeech, meaningEn, hanViet, audioUrl);
        return unit;
    }

    public static KnowledgeUnit CreateGrammarPattern(
        string code,
        string pattern,
        string explanationVi,
        byte? hskLevel = null,
        string? explanationEn = null)
    {
        var unit = new KnowledgeUnit(NewId(), KnowledgeType.Grammar, Guard.NotBlank(code));
        unit.GrammarPattern = GrammarPattern.Create(unit.Id, pattern, explanationVi, hskLevel, explanationEn);
        return unit;
    }

    /// <summary>Records that learning content changed; activities snapshot the version they were planned with.</summary>
    public void IncrementContentVersion() => ContentVersion++;

    public void Archive() => Status = KnowledgeUnitStatus.Archived;
}
