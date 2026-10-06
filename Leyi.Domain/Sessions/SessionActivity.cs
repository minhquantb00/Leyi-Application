using System.Text.Json;
using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Sessions;

/// <summary>
/// One planned activity of a <see cref="LearningSession"/> (ERD §6.19). Holds an immutable content snapshot taken
/// at planning time. Assessment activities receive attempts; NEW_VOCABULARY_EXPOSURE is completed without attempts.
/// State changes go through the session aggregate.
/// </summary>
public sealed class SessionActivity : Entity
{
    public const decimal MinPriorityScore = 0m;

    public const decimal MaxPriorityScore = 100m;

    private readonly List<SessionActivityReason> reasons = [];
    private readonly List<ActivityAttempt> attempts = [];

    private SessionActivity()
    {
    }

    private SessionActivity(Guid id)
        : base(id)
    {
    }

    public Guid LearningSessionId { get; private set; }

    /// <summary>Target knowledge unit. Nullable in the schema for future non-knowledge activities; required by every V1 activity type.</summary>
    public Guid? KnowledgeUnitId { get; private set; }

    public ActivityType ActivityType { get; private set; }

    public MasteryDimension? TargetDimension { get; private set; }

    public short SortOrder { get; private set; }

    public short EstimatedDurationSeconds { get; private set; }

    public decimal? PriorityScore { get; private set; }

    public ActivityStatus Status { get; private set; }

    /// <summary>Opaque, versioned JSON snapshot (prompt, options, accepted answers).</summary>
    public string ContentSnapshot { get; private set; } = string.Empty;

    public int ContentVersion { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<SessionActivityReason> Reasons => reasons;

    public IReadOnlyCollection<ActivityAttempt> Attempts => attempts;

    public bool IsAssessment => ActivityType.IsAssessment();

    /// <summary>Planned or started, i.e. still able to become the current activity.</summary>
    public bool IsPending => Status is ActivityStatus.Planned or ActivityStatus.InProgress;

    /// <summary>
    /// Closing rule (Learning Model §33.1): an assessment activity closes when answered correctly, when the answer is
    /// shown, or when the last allowed attempt is used.
    /// </summary>
    public static bool ClosesActivity(bool correct, bool answerShown, int attemptNumber, int maxAttempts) =>
        correct || answerShown || attemptNumber >= maxAttempts;

    internal static SessionActivity Create(Guid learningSessionId, short sortOrder, ActivityPlan plan)
    {
        var activityType = Guard.Defined(plan.ActivityType, nameof(plan.ActivityType));

        if (activityType.IsAssessment())
        {
            if (plan.TargetDimension is null)
            {
                throw new ArgumentException("Assessment activities require a target (primary) dimension.", nameof(plan));
            }

            Guard.Defined(plan.TargetDimension.Value, nameof(plan.TargetDimension));
        }
        else if (plan.TargetDimension is not null)
        {
            throw new ArgumentException($"{activityType} is not an assessment and has no target dimension.", nameof(plan));
        }

        if (plan.EstimatedDurationSeconds is <= 0 or > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), plan.EstimatedDurationSeconds, "Estimated duration must be a positive number of seconds.");
        }

        if (plan.PriorityScore is < MinPriorityScore or > MaxPriorityScore)
        {
            throw new ArgumentOutOfRangeException(nameof(plan), plan.PriorityScore, "Priority score must be between 0 and 100.");
        }

        Guard.Positive(plan.ContentVersion, nameof(plan.ContentVersion));
        ArgumentNullException.ThrowIfNull(plan.Reasons);

        if (plan.Reasons.Count == 0)
        {
            throw new ArgumentException("A selected activity must record at least one selection reason.", nameof(plan));
        }

        if (plan.Reasons.Select(reason => reason.Reason).Distinct().Count() != plan.Reasons.Count)
        {
            throw new ArgumentException("Selection reasons must be distinct.", nameof(plan));
        }

        var activity = new SessionActivity(NewId())
        {
            LearningSessionId = learningSessionId,
            KnowledgeUnitId = Guard.NotEmpty(plan.KnowledgeUnitId, nameof(plan.KnowledgeUnitId)),
            ActivityType = activityType,
            TargetDimension = plan.TargetDimension,
            SortOrder = sortOrder,
            EstimatedDurationSeconds = (short)plan.EstimatedDurationSeconds,
            PriorityScore = plan.PriorityScore,
            Status = ActivityStatus.Planned,
            ContentSnapshot = ValidateSnapshot(plan.ContentSnapshot),
            ContentVersion = plan.ContentVersion,
        };

        foreach (var reason in plan.Reasons)
        {
            activity.reasons.Add(SessionActivityReason.Create(activity.Id, reason));
        }

        return activity;
    }

    internal ActivityAttempt AddAttempt(AttemptSubmission submission, int maxAttempts, EvidenceQuality? closingQuality, DateTime nowUtc)
    {
        var attemptNumber = attempts.Count + 1;
        if (attemptNumber > maxAttempts || attemptNumber > byte.MaxValue)
        {
            throw new DomainException(DomainErrorCodes.ActivityNotActive, "The activity has no attempts left.");
        }

        var closes = ClosesActivity(submission.Correct, submission.AnswerShown, attemptNumber, maxAttempts);
        if (closes && closingQuality is null)
        {
            throw new ArgumentException("The attempt closes the activity, so its quality is required.", nameof(closingQuality));
        }

        if (!closes && closingQuality is not null)
        {
            throw new ArgumentException("Only the attempt that closes the activity carries a quality.", nameof(closingQuality));
        }

        var attempt = ActivityAttempt.Create(Id, (byte)attemptNumber, submission, closingQuality, nowUtc);
        attempts.Add(attempt);

        if (closes)
        {
            Close(nowUtc);
        }
        else
        {
            Status = ActivityStatus.InProgress;
        }

        return attempt;
    }

    internal void Close(DateTime nowUtc)
    {
        Status = ActivityStatus.Completed;
        CompletedAt = nowUtc;
    }

    internal void Cancel() => Status = ActivityStatus.Cancelled;

    private static string ValidateSnapshot(string snapshot)
    {
        Guard.NotBlank(snapshot, nameof(snapshot));

        try
        {
            using var document = JsonDocument.Parse(snapshot);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("The content snapshot must be a JSON object.", nameof(snapshot));
            }
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("The content snapshot must be valid JSON.", nameof(snapshot), exception);
        }

        return snapshot;
    }
}
