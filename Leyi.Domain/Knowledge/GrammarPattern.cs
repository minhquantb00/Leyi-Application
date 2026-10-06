using Leyi.Domain.Common;

namespace Leyi.Domain.Knowledge;

/// <summary>Grammar subtype of <see cref="KnowledgeUnit"/> (ERD §6.10). Key = KnowledgeUnitId.</summary>
public sealed class GrammarPattern
{
    private GrammarPattern()
    {
    }

    public Guid KnowledgeUnitId { get; private set; }

    public string Pattern { get; private set; } = string.Empty;

    public string ExplanationVi { get; private set; } = string.Empty;

    public string? ExplanationEn { get; private set; }

    public byte? HskLevel { get; private set; }

    internal static GrammarPattern Create(Guid knowledgeUnitId, string pattern, string explanationVi, byte? hskLevel, string? explanationEn) => new()
    {
        KnowledgeUnitId = knowledgeUnitId,
        Pattern = Guard.NotBlank(pattern),
        ExplanationVi = Guard.NotBlank(explanationVi),
        HskLevel = HskLevels.Validate(hskLevel, nameof(hskLevel)),
        ExplanationEn = Guard.NullIfBlank(explanationEn),
    };
}
