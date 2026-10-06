using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Mastery;

/// <summary>
/// Aggregate root: current mastery of one knowledge unit for one user (Domain Model §7, ERD §6.15).
/// Created at the First Learning Interaction (exposure or first assessment); "NEW" means no row exists.
/// Holds state and invariants only. Scores, state and confidence are computed by the MasteryCalculator,
/// KnowledgeStateEvaluator and policies, then applied through the explicit methods below.
/// </summary>
public sealed class UserKnowledgeMastery : Entity
{
    private readonly List<UserKnowledgeDimensionMastery> dimensions = [];

    private UserKnowledgeMastery()
    {
    }

    private UserKnowledgeMastery(Guid id, Guid userId, Guid knowledgeUnitId, DateTime firstSeenAt)
        : base(id)
    {
        UserId = userId;
        KnowledgeUnitId = knowledgeUnitId;
        KnowledgeState = KnowledgeState.Learning;
        ConfidenceLevel = ConfidenceLevel.Low;
        FirstSeenAt = firstSeenAt;
    }

    public Guid UserId { get; private set; }

    public Guid KnowledgeUnitId { get; private set; }

    public KnowledgeState KnowledgeState { get; private set; }

    /// <summary>Cached derived value for display; never the sole signal for selecting activities.</summary>
    public decimal OverallMastery { get; private set; }

    public ConfidenceLevel ConfidenceLevel { get; private set; }

    public DateTime FirstSeenAt { get; private set; }

    /// <summary>Time of the latest evidence.</summary>
    public DateTime? LastPracticedAt { get; private set; }

    /// <summary>Latest Q0 evidence on any dimension (denormalized for the Recent-Error pool; not a counter).</summary>
    public DateTime? LastErrorAt { get; private set; }

    public int TotalEvidenceCount { get; private set; }

    /// <summary>Number of learner-local days with evidence; increases only when the local date moves forward (B8).</summary>
    public int EvidenceDayCount { get; private set; }

    public DateOnly? LastEvidenceLocalDate { get; private set; }

    public IReadOnlyCollection<UserKnowledgeDimensionMastery> Dimensions => dimensions;

    public ReviewSchedule ReviewSchedule { get; private set; } = null!;

    /// <summary>
    /// First Learning Interaction: creates the aggregate in LEARNING with one row per applicable dimension
    /// (unmeasured) and a level-0 review schedule due after <paramref name="level0Interval"/>.
    /// </summary>
    public static UserKnowledgeMastery Start(
        Guid userId,
        Guid knowledgeUnitId,
        IReadOnlyCollection<MasteryDimension> applicableDimensions,
        DateTime nowUtc,
        TimeSpan level0Interval)
    {
        ArgumentNullException.ThrowIfNull(applicableDimensions);

        if (applicableDimensions.Count == 0 || applicableDimensions.Distinct().Count() != applicableDimensions.Count)
        {
            throw new ArgumentException("Applicable dimensions must be a non-empty set.", nameof(applicableDimensions));
        }

        var mastery = new UserKnowledgeMastery(NewId(), Guard.NotEmpty(userId), Guard.NotEmpty(knowledgeUnitId), Guard.Utc(nowUtc));

        foreach (var dimension in applicableDimensions)
        {
            mastery.dimensions.Add(UserKnowledgeDimensionMastery.Create(mastery.Id, dimension));
        }

        mastery.ReviewSchedule = ReviewSchedule.Create(mastery.Id, userId, nowUtc, level0Interval);
        return mastery;
    }

    public UserKnowledgeDimensionMastery GetDimension(MasteryDimension dimension) =>
        dimensions.SingleOrDefault(item => item.Dimension == dimension)
        ?? throw new DomainException(DomainErrorCodes.MasteryStateInvalid, $"Dimension {dimension} does not apply to this knowledge unit.");

    /// <summary>
    /// Applies one evidence to its primary dimension only; other dimensions are unchanged.
    /// <paramref name="newScore"/> is computed by the MasteryCalculator. <paramref name="evidenceLocalDate"/>
    /// is the learner-local date snapshotted on the evidence.
    /// </summary>
    public void RecordEvidence(
        MasteryDimension dimension,
        decimal newScore,
        EvidenceQuality quality,
        bool finalCorrect,
        DateTime occurredAtUtc,
        DateOnly evidenceLocalDate)
    {
        Guard.Defined(quality);
        Guard.Utc(occurredAtUtc);

        if (!finalCorrect && quality != EvidenceQuality.Q0)
        {
            throw new DomainException(DomainErrorCodes.EvidenceInvalid, "An incorrect final result must have quality Q0.");
        }

        GetDimension(dimension).ApplyEvidence(newScore, quality, finalCorrect, occurredAtUtc);

        TotalEvidenceCount++;
        LastPracticedAt = occurredAtUtc;

        if (quality == EvidenceQuality.Q0)
        {
            LastErrorAt = occurredAtUtc;
        }

        // V1 approximation (B8): count a new evidence day only when the local date moves forward,
        // so a timezone change that moves the date backwards never inflates the counter.
        if (LastEvidenceLocalDate is null || evidenceLocalDate > LastEvidenceLocalDate.Value)
        {
            EvidenceDayCount++;
            LastEvidenceLocalDate = evidenceLocalDate;
        }
    }

    /// <summary>Applies the result of the KnowledgeStateEvaluator / ConfidencePolicy / overall weighting.</summary>
    public void ApplyEvaluation(KnowledgeState state, ConfidenceLevel confidence, decimal overallMastery)
    {
        if (Guard.Defined(state) == KnowledgeState.New)
        {
            throw new DomainException(DomainErrorCodes.MasteryStateInvalid, "NEW is derived (no mastery row) and cannot be assigned.");
        }

        if (state != KnowledgeState.Learning && TotalEvidenceCount == 0)
        {
            throw new DomainException(DomainErrorCodes.MasteryStateInvalid, $"State {state} requires learning evidence.");
        }

        KnowledgeState = state;
        ConfidenceLevel = Guard.Defined(confidence);
        OverallMastery = UserKnowledgeDimensionMastery.ValidateScore(overallMastery);
    }
}
