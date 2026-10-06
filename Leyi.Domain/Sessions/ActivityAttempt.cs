using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Sessions;

/// <summary>
/// What the learner actually did for an assessment activity (ERD §6.21). Created only through
/// <see cref="LearningSession.RecordAttempt"/>. Only the attempt that closes the activity carries a quality.
/// </summary>
public sealed class ActivityAttempt : Entity
{
    private ActivityAttempt()
    {
    }

    private ActivityAttempt(Guid id)
        : base(id)
    {
    }

    public Guid SessionActivityId { get; private set; }

    /// <summary>1-based, sequential within the activity.</summary>
    public byte AttemptNumber { get; private set; }

    public string? UserAnswer { get; private set; }

    public bool Correct { get; private set; }

    public HintLevel HintLevel { get; private set; }

    public bool AnswerShown { get; private set; }

    public int? ResponseTimeMs { get; private set; }

    public short? AudioReplayCount { get; private set; }

    /// <summary>Activity outcome quality; set only on the attempt that closes the activity.</summary>
    public EvidenceQuality? Quality { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime CompletedAt { get; private set; }

    internal static ActivityAttempt Create(
        Guid sessionActivityId,
        byte attemptNumber,
        AttemptSubmission submission,
        EvidenceQuality? closingQuality,
        DateTime completedAtUtc)
    {
        if (submission.ResponseTimeMs is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(submission), submission.ResponseTimeMs, "Response time must not be negative.");
        }

        if (submission.AudioReplayCount is < 0 or > AttemptSubmission.MaxAudioReplayCount)
        {
            throw new ArgumentOutOfRangeException(nameof(submission), submission.AudioReplayCount, $"Audio replay count must be between 0 and {AttemptSubmission.MaxAudioReplayCount}.");
        }

        var startedAt = Guard.Utc(submission.StartedAtUtc, nameof(submission.StartedAtUtc));
        if (startedAt > completedAtUtc)
        {
            throw new ArgumentException("An attempt cannot start after it completes.", nameof(submission));
        }

        return new ActivityAttempt(NewId())
        {
            SessionActivityId = sessionActivityId,
            AttemptNumber = attemptNumber,
            UserAnswer = submission.UserAnswer,
            Correct = submission.Correct,
            HintLevel = Guard.Defined(submission.HintLevel, nameof(submission.HintLevel)),
            AnswerShown = submission.AnswerShown,
            ResponseTimeMs = submission.ResponseTimeMs,
            AudioReplayCount = (short?)submission.AudioReplayCount,
            Quality = closingQuality is null ? null : Guard.Defined(closingQuality.Value, nameof(closingQuality)),
            StartedAt = startedAt,
            CompletedAt = completedAtUtc,
        };
    }
}
