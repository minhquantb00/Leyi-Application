using Leyi.Domain.Learning;

namespace Leyi.Domain.Evidence;

/// <summary>Input for <see cref="LearningEvidence.Record"/>; produced by the EvidenceFactory from a closed activity.</summary>
public sealed record LearningEvidenceFacts
{
    public required Guid UserId { get; init; }

    public required Guid KnowledgeUnitId { get; init; }

    public Guid? ActivityAttemptId { get; init; }

    public required MasteryDimension Dimension { get; init; }

    public required EvidenceSourceType SourceType { get; init; }

    public Guid? SourceId { get; init; }

    public required ActivityType ActivityType { get; init; }

    public required EvidenceQuality Quality { get; init; }

    public required bool InitialCorrect { get; init; }

    public required bool Correct { get; init; }

    public HintLevel HintLevel { get; init; } = HintLevel.None;

    public bool AnswerShown { get; init; }

    public int? ResponseTimeMs { get; init; }

    public required decimal EvidenceWeight { get; init; }

    public decimal SessionDiscountFactor { get; init; } = 1m;

    public string? PolicyVersion { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateOnly LocalDate { get; init; }

    public required string TimeZoneId { get; init; }
}
