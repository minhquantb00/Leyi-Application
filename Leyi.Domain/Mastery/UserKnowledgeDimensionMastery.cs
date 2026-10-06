using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Mastery;

/// <summary>
/// Current state of one mastery dimension (ERD §6.16), child of <see cref="UserKnowledgeMastery"/>.
/// <see cref="EvidenceCount"/> = 0 means the dimension is unmeasured. RecentErrorCount is not stored:
/// it is computed from LearningEvidence (Q0 within the recent-error window, B7).
/// </summary>
public sealed class UserKnowledgeDimensionMastery : Entity
{
    public const decimal MinScore = 0m;

    public const decimal MaxScore = 100m;

    private UserKnowledgeDimensionMastery()
    {
    }

    private UserKnowledgeDimensionMastery(Guid id, Guid userKnowledgeMasteryId, MasteryDimension dimension)
        : base(id)
    {
        UserKnowledgeMasteryId = userKnowledgeMasteryId;
        Dimension = dimension;
    }

    public Guid UserKnowledgeMasteryId { get; private set; }

    public MasteryDimension Dimension { get; private set; }

    public decimal Score { get; private set; }

    public int EvidenceCount { get; private set; }

    public DateTime? LastPracticedAt { get; private set; }

    public DateTime? LastCorrectAt { get; private set; }

    /// <summary>Most recent Q0 evidence (denormalized ranking field).</summary>
    public DateTime? LastErrorAt { get; private set; }

    public bool IsMeasured => EvidenceCount > 0;

    internal static UserKnowledgeDimensionMastery Create(Guid userKnowledgeMasteryId, MasteryDimension dimension) =>
        new(NewId(), userKnowledgeMasteryId, Guard.Defined(dimension));

    internal void ApplyEvidence(decimal newScore, EvidenceQuality quality, bool finalCorrect, DateTime occurredAtUtc)
    {
        Score = ValidateScore(newScore);
        EvidenceCount++;
        LastPracticedAt = occurredAtUtc;

        if (finalCorrect)
        {
            LastCorrectAt = occurredAtUtc;
        }

        if (quality == EvidenceQuality.Q0)
        {
            LastErrorAt = occurredAtUtc;
        }
    }

    internal static decimal ValidateScore(decimal score) =>
        score is < MinScore or > MaxScore
            ? throw new DomainException(DomainErrorCodes.MasteryStateInvalid, $"Mastery score must be between {MinScore} and {MaxScore}.")
            : decimal.Round(score, 2, MidpointRounding.AwayFromZero);
}
