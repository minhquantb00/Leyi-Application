using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Mastery;

/// <summary>
/// SRS state of one knowledge unit for one user (ERD §6.17), part of the <see cref="UserKnowledgeMastery"/> aggregate.
/// The entity protects level bounds and the outcome/quality relationship (Learning Model §24.1);
/// deciding the outcome, credit eligibility and the schedule anchor belongs to the ReviewScheduler.
/// </summary>
public sealed class ReviewSchedule : Entity
{
    private ReviewSchedule()
    {
    }

    private ReviewSchedule(Guid id, Guid userKnowledgeMasteryId, Guid userId, DateTime nextReviewAt)
        : base(id)
    {
        UserKnowledgeMasteryId = userKnowledgeMasteryId;
        UserId = userId;
        ReviewLevel = ReviewLevel.Initial;
        NextReviewAt = nextReviewAt;
    }

    public Guid UserKnowledgeMasteryId { get; private set; }

    /// <summary>Denormalized owner (= mastery.UserId, immutable) for per-user due queries.</summary>
    public Guid UserId { get; private set; }

    public ReviewLevel ReviewLevel { get; private set; }

    public DateTime? LastReviewedAt { get; private set; }

    /// <summary>Exact UTC timestamp. DueNow when <c>NextReviewAt &lt;= now</c>.</summary>
    public DateTime NextReviewAt { get; private set; }

    public EvidenceQuality? LastResultQuality { get; private set; }

    public short ConsecutiveSuccess { get; private set; }

    public short ConsecutiveFailure { get; private set; }

    /// <summary>DueNow (Learning Model §22.1). Applies to every level, including level 0 reinforcement.</summary>
    public bool IsDueNow(DateTime nowUtc) => NextReviewAt <= Guard.Utc(nowUtc);

    /// <summary>ReviewDebt membership: level ≥ 1 and DueNow. Level 0 and DueToday never count.</summary>
    public bool CountsTowardReviewDebt(DateTime nowUtc) => ReviewLevel.IsInReviewCycle && IsDueNow(nowUtc);

    /// <summary>Promote: +1 level (stays at 7). Only for Q3/Q4 results (Learning Model §24.1).</summary>
    public void Promote(EvidenceQuality quality, DateTime nextReviewAtUtc, DateTime nowUtc)
    {
        if (quality is not (EvidenceQuality.Q3 or EvidenceQuality.Q4))
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, $"Promotion requires Q3 or Q4, not {quality}.");
        }

        var nextLevel = ReviewLevel.Value == ReviewLevel.MaxValue ? ReviewLevel.Highest : ReviewLevel.From(ReviewLevel.Value + 1);
        ApplySuccess(nextLevel, quality, nextReviewAtUtc, nowUtc);
    }

    /// <summary>Maintain: keep the level, reschedule. Only for correct (Q2–Q4) results on items in the review cycle.</summary>
    public void Maintain(EvidenceQuality quality, DateTime nextReviewAtUtc, DateTime nowUtc)
    {
        if (quality is not (EvidenceQuality.Q2 or EvidenceQuality.Q3 or EvidenceQuality.Q4))
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, $"Maintain requires a correct result (Q2–Q4), not {quality}.");
        }

        if (!ReviewLevel.IsInReviewCycle)
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, "Level 0 items are promoted or left unchanged, never maintained.");
        }

        ApplySuccess(ReviewLevel, quality, nextReviewAtUtc, nowUtc);
    }

    /// <summary>
    /// Demote by <paramref name="steps"/> levels, not below <paramref name="floor"/> (Q1 → −1, Q0 → −2, floor 1 in SrsPolicy V1).
    /// A level-0 item stays at level 0.
    /// </summary>
    public void Demote(EvidenceQuality quality, int steps, ReviewLevel floor, DateTime nextReviewAtUtc, DateTime nowUtc)
    {
        if (quality is not (EvidenceQuality.Q0 or EvidenceQuality.Q1))
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, $"Demotion requires Q0 or Q1, not {quality}.");
        }

        Guard.Positive(steps);

        var nextLevel = ReviewLevel.IsInReviewCycle
            ? ReviewLevel.From(Math.Max(ReviewLevel.Value - steps, floor.Value))
            : ReviewLevel;

        if (nextLevel.Value > ReviewLevel.Value)
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, "Demotion cannot raise the review level.");
        }

        Reschedule(nextLevel, quality, nextReviewAtUtc, nowUtc);
        ConsecutiveSuccess = 0;
        ConsecutiveFailure = Increment(ConsecutiveFailure);
    }

    internal static ReviewSchedule Create(Guid userKnowledgeMasteryId, Guid userId, DateTime nowUtc, TimeSpan level0Interval) =>
        new(NewId(), userKnowledgeMasteryId, userId, Guard.Utc(nowUtc) + Guard.Positive(level0Interval));

    private void ApplySuccess(ReviewLevel nextLevel, EvidenceQuality quality, DateTime nextReviewAtUtc, DateTime nowUtc)
    {
        Reschedule(nextLevel, quality, nextReviewAtUtc, nowUtc);
        ConsecutiveSuccess = Increment(ConsecutiveSuccess);
        ConsecutiveFailure = 0;
    }

    private void Reschedule(ReviewLevel nextLevel, EvidenceQuality quality, DateTime nextReviewAtUtc, DateTime nowUtc)
    {
        Guard.Utc(nowUtc);
        if (Guard.Utc(nextReviewAtUtc) <= nowUtc)
        {
            throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, "The next review must be scheduled in the future.");
        }

        ReviewLevel = nextLevel;
        NextReviewAt = nextReviewAtUtc;
        LastReviewedAt = nowUtc;
        LastResultQuality = Guard.Defined(quality);
    }

    private static short Increment(short value) => value == short.MaxValue ? value : (short)(value + 1);
}
