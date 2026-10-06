using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Evidence;

/// <summary>
/// Immutable historical learning fact (ERD §6.22, Domain Model §12). V1: one evidence per closed assessment
/// activity, for its primary dimension only; NEW_VOCABULARY_EXPOSURE never produces evidence.
/// <see cref="LocalDate"/>/<see cref="TimeZoneId"/> are snapshotted at creation and never recomputed.
/// Source of truth for RecentErrorCount (Q0 within the recent-error window).
/// </summary>
public sealed class LearningEvidence : Entity
{
    public const int MaxTimeZoneIdLength = 64;

    private LearningEvidence()
    {
    }

    private LearningEvidence(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public Guid KnowledgeUnitId { get; private set; }

    /// <summary>Attempt that closed the activity (null for sources without attempts).</summary>
    public Guid? ActivityAttemptId { get; private set; }

    public MasteryDimension Dimension { get; private set; }

    public EvidenceSourceType SourceType { get; private set; }

    public Guid? SourceId { get; private set; }

    public ActivityType ActivityType { get; private set; }

    public EvidenceQuality Quality { get; private set; }

    /// <summary>Result of the first attempt.</summary>
    public bool InitialCorrect { get; private set; }

    /// <summary>Final result of the activity.</summary>
    public bool Correct { get; private set; }

    /// <summary>Highest hint level used across the activity's attempts.</summary>
    public HintLevel HintLevel { get; private set; }

    public bool AnswerShown { get; private set; }

    public int? ResponseTimeMs { get; private set; }

    /// <summary>EvidenceWeight of the activity type (0–1) under <see cref="PolicyVersion"/>.</summary>
    public decimal EvidenceWeight { get; private set; }

    /// <summary>Same-session discount actually applied (1.0 when the evidence was not a gain).</summary>
    public decimal SessionDiscountFactor { get; private set; }

    public string? PolicyVersion { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateOnly LocalDate { get; private set; }

    public string TimeZoneId { get; private set; } = string.Empty;

    public static LearningEvidence Record(LearningEvidenceFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (!facts.ActivityType.IsAssessment())
        {
            throw new DomainException(DomainErrorCodes.ActivityTypeNotSupported, $"{facts.ActivityType} is not an assessment and cannot produce evidence.");
        }

        Guard.Defined(facts.Quality);
        Guard.Defined(facts.HintLevel);
        EnsureQualityConsistent(facts);

        if (facts.ResponseTimeMs is < 0)
        {
            throw new DomainException(DomainErrorCodes.EvidenceInvalid, "Response time must not be negative.");
        }

        var timeZoneId = Guard.NotBlank(facts.TimeZoneId, nameof(facts.TimeZoneId));
        if (timeZoneId.Length > MaxTimeZoneIdLength)
        {
            throw new DomainException(DomainErrorCodes.EvidenceInvalid, "Time zone id is too long.");
        }

        return new LearningEvidence(NewId())
        {
            UserId = Guard.NotEmpty(facts.UserId, nameof(facts.UserId)),
            KnowledgeUnitId = Guard.NotEmpty(facts.KnowledgeUnitId, nameof(facts.KnowledgeUnitId)),
            ActivityAttemptId = facts.ActivityAttemptId,
            Dimension = Guard.Defined(facts.Dimension, nameof(facts.Dimension)),
            SourceType = Guard.Defined(facts.SourceType, nameof(facts.SourceType)),
            SourceId = facts.SourceId,
            ActivityType = facts.ActivityType,
            Quality = facts.Quality,
            InitialCorrect = facts.InitialCorrect,
            Correct = facts.Correct,
            HintLevel = facts.HintLevel,
            AnswerShown = facts.AnswerShown,
            ResponseTimeMs = facts.ResponseTimeMs,
            EvidenceWeight = Fraction(facts.EvidenceWeight, nameof(facts.EvidenceWeight)),
            SessionDiscountFactor = Fraction(facts.SessionDiscountFactor, nameof(facts.SessionDiscountFactor)),
            PolicyVersion = Guard.NullIfBlank(facts.PolicyVersion),
            CreatedAt = Guard.Utc(facts.CreatedAtUtc, nameof(facts.CreatedAtUtc)),
            LocalDate = facts.LocalDate,
            TimeZoneId = timeZoneId,
        };
    }

    /// <summary>
    /// Structural consistency of the Quality Cap Rule (Learning Model §12.1). Slow/replay caps depend on
    /// policy baselines and are applied by the ActivityEvaluator, not checked here.
    /// </summary>
    private static void EnsureQualityConsistent(LearningEvidenceFacts facts)
    {
        var maxQuality = EvidenceQuality.Q4;

        if (facts.AnswerShown || !facts.Correct)
        {
            maxQuality = EvidenceQuality.Q0;
        }
        else
        {
            if (!facts.InitialCorrect || facts.HintLevel == HintLevel.Level2)
            {
                maxQuality = EvidenceQuality.Q1;
            }
            else if (facts.HintLevel == HintLevel.Level1)
            {
                maxQuality = EvidenceQuality.Q2;
            }

            if (facts.Quality == EvidenceQuality.Q0)
            {
                throw new DomainException(DomainErrorCodes.EvidenceInvalid, "A correct result without a shown answer cannot be Q0.");
            }
        }

        if (facts.Quality > maxQuality)
        {
            throw new DomainException(DomainErrorCodes.EvidenceInvalid, $"Quality {facts.Quality} exceeds the cap {maxQuality} for this result.");
        }
    }

    private static decimal Fraction(decimal value, string name) =>
        value is < 0m or > 1m
            ? throw new DomainException(DomainErrorCodes.EvidenceInvalid, $"{name} must be between 0 and 1.")
            : value;
}
