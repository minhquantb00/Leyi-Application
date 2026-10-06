using Leyi.Domain.Common;
using Leyi.Domain.Learning;

namespace Leyi.Domain.Sessions;

/// <summary>
/// Aggregate root for one learning session (Domain Model §10, ERD §6.18).
/// Lifecycle (C2/C5): Create (GENERATED) → Start → IN_PROGRESS ⇄ PAUSED → COMPLETED, with explicit ABANDON and
/// idle-based EXPIRE. Activities run in strict SortOrder. Active time accrues incrementally, capped per gap.
/// Expiry is evaluated against the injected <see cref="SessionTimingRules"/>; an idle-expired session rejects
/// commands with SESSION_EXPIRED until <see cref="ExpireIfIdle"/> materializes it.
/// </summary>
public sealed class LearningSession : Entity
{
    private readonly List<SessionActivity> activities = [];

    private LearningSession()
    {
    }

    private LearningSession(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public SessionType SessionType { get; private set; }

    public short? RequestedDurationSeconds { get; private set; }

    public short? EffectiveBudgetSeconds { get; private set; }

    /// <summary>Meaningful elapsed time: sum of gaps between interactions, each capped by the idle cap.</summary>
    public int ActualDurationSeconds { get; private set; }

    /// <summary>Skill constraint of a PRACTICE session.</summary>
    public MasteryDimension? RequestedSkill { get; private set; }

    public SessionStatus Status { get; private set; }

    /// <summary>Adaptive policy version used to generate the plan.</summary>
    public string? GenerationVersion { get; private set; }

    /// <summary>Closed assessment activities (each produced evidence); ≥ 3 makes an ended session "meaningful".</summary>
    public short EvaluatedActivityCount { get; private set; }

    public DateTime GeneratedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? PausedAt { get; private set; }

    public DateTime? LastActivityAt { get; private set; }

    /// <summary>Set when the session ends: COMPLETED, ABANDONED or EXPIRED.</summary>
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<SessionActivity> Activities => activities;

    /// <summary>Strict ordering: the pending activity with the lowest SortOrder.</summary>
    public SessionActivity? CurrentActivity =>
        activities.Where(activity => activity.IsPending).MinBy(activity => activity.SortOrder);

    public static LearningSession Create(
        Guid userId,
        SessionType sessionType,
        DateTime nowUtc,
        string? generationVersion,
        int? requestedDurationSeconds = null,
        int? effectiveBudgetSeconds = null,
        MasteryDimension? requestedSkill = null)
    {
        Guard.Defined(sessionType);

        if (sessionType == SessionType.Practice && requestedSkill is null)
        {
            throw new ArgumentException("A practice session requires a requested skill.", nameof(requestedSkill));
        }

        if (sessionType != SessionType.Practice && requestedSkill is not null)
        {
            throw new ArgumentException("Only practice sessions have a requested skill.", nameof(requestedSkill));
        }

        var requested = ToSeconds(requestedDurationSeconds, nameof(requestedDurationSeconds));
        var budget = ToSeconds(effectiveBudgetSeconds, nameof(effectiveBudgetSeconds));
        if (budget > requested)
        {
            throw new ArgumentException("The effective budget cannot exceed the requested duration.", nameof(effectiveBudgetSeconds));
        }

        return new LearningSession(NewId())
        {
            UserId = Guard.NotEmpty(userId),
            SessionType = sessionType,
            RequestedDurationSeconds = requested,
            EffectiveBudgetSeconds = budget,
            RequestedSkill = requestedSkill is null ? null : Guard.Defined(requestedSkill.Value, nameof(requestedSkill)),
            Status = SessionStatus.Generated,
            GenerationVersion = Guard.NullIfBlank(generationVersion),
            GeneratedAt = Guard.Utc(nowUtc),
        };
    }

    /// <summary>
    /// Appends a planned activity after all existing ones (SortOrder &gt; max; never renumbered).
    /// Allowed while GENERATED (initial plan) or IN_PROGRESS (re-planning).
    /// </summary>
    public SessionActivity AddActivity(ActivityPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (Status is not (SessionStatus.Generated or SessionStatus.InProgress))
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"Activities cannot be added to a {Status} session.");
        }

        var nextSortOrder = activities.Count == 0 ? 1 : activities.Max(activity => activity.SortOrder) + 1;
        if (nextSortOrder > short.MaxValue)
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, "The session cannot hold more activities.");
        }

        var activity = SessionActivity.Create(Id, (short)nextSortOrder, plan);
        activities.Add(activity);
        return activity;
    }

    /// <summary>GENERATED → IN_PROGRESS. Requires at least one pending activity.</summary>
    public void Start(DateTime nowUtc)
    {
        Guard.Utc(nowUtc);

        if (Status != SessionStatus.Generated)
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"Only a generated session can start; status is {Status}.");
        }

        if (!activities.Any(activity => activity.IsPending))
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, "A session cannot start without planned activities.");
        }

        Status = SessionStatus.InProgress;
        StartedAt = nowUtc;
        LastActivityAt = nowUtc;
    }

    public bool IsIdleExpired(DateTime nowUtc, SessionTimingRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return Status.IsOpen() && Guard.Utc(nowUtc) - IdleReference > rules.ExpiryThreshold;
    }

    public ResumeMode GetResumeMode(DateTime nowUtc, SessionTimingRules rules)
    {
        EnsureUsable(nowUtc, rules);
        return nowUtc - IdleReference <= rules.ContinueThreshold ? ResumeMode.Continue : ResumeMode.RegenerateRemaining;
    }

    /// <summary>Records an evaluated attempt on the current assessment activity. Accrues active time.</summary>
    public AttemptRecorded RecordAttempt(
        Guid activityId,
        AttemptSubmission submission,
        int maxAttempts,
        EvidenceQuality? closingQuality,
        DateTime nowUtc,
        SessionTimingRules rules)
    {
        ArgumentNullException.ThrowIfNull(submission);
        Guard.Positive(maxAttempts);
        EnsureInProgress(nowUtc, rules);

        var activity = GetCurrentActivity(activityId);
        if (!activity.IsAssessment)
        {
            throw new DomainException(DomainErrorCodes.ActivityTypeNotSupported, $"{activity.ActivityType} is not an assessment; complete it instead.");
        }

        var attempt = activity.AddAttempt(submission, maxAttempts, closingQuality, nowUtc);
        var closed = activity.Status == ActivityStatus.Completed;
        if (closed)
        {
            EvaluatedActivityCount++;
        }

        return new AttemptRecorded(attempt, closed, AccrueActiveTime(nowUtc, rules));
    }

    /// <summary>
    /// Completes the current non-assessment activity (NEW_VOCABULARY_EXPOSURE). Idempotent: completing an
    /// already completed activity changes nothing. Produces no attempt and no evidence.
    /// </summary>
    public ActivityCompletion CompleteNonAssessmentActivity(Guid activityId, DateTime nowUtc, SessionTimingRules rules)
    {
        var existing = activities.SingleOrDefault(activity => activity.Id == activityId)
            ?? throw new DomainException(DomainErrorCodes.ActivityNotFound, "The activity does not belong to this session.");

        if (existing.Status == ActivityStatus.Completed && !existing.IsAssessment)
        {
            return new ActivityCompletion(AlreadyCompleted: true, AccruedSeconds: 0);
        }

        EnsureInProgress(nowUtc, rules);

        var activity = GetCurrentActivity(activityId);
        if (activity.IsAssessment)
        {
            throw new DomainException(DomainErrorCodes.ActivityTypeNotSupported, $"{activity.ActivityType} is an assessment; submit an attempt instead.");
        }

        activity.Close(nowUtc);
        return new ActivityCompletion(AlreadyCompleted: false, AccruedSeconds: AccrueActiveTime(nowUtc, rules));
    }

    /// <summary>IN_PROGRESS → PAUSED; accrues tail time. Idempotent when already paused (returns 0).</summary>
    public int Pause(DateTime nowUtc, SessionTimingRules rules)
    {
        EnsureUsable(nowUtc, rules);

        if (Status == SessionStatus.Paused)
        {
            return 0;
        }

        EnsureStatus(SessionStatus.InProgress);

        var accrued = AccrueActiveTime(nowUtc, rules);
        Status = SessionStatus.Paused;
        PausedAt = nowUtc;
        return accrued;
    }

    /// <summary>
    /// PAUSED (or interrupted IN_PROGRESS) → IN_PROGRESS. Pause time is never credited. When the gap exceeds the
    /// continue threshold, pending activities are cancelled so the caller can append a re-planned remainder.
    /// </summary>
    public ResumeMode Resume(DateTime nowUtc, SessionTimingRules rules)
    {
        var mode = GetResumeMode(nowUtc, rules);

        if (Status is not (SessionStatus.Paused or SessionStatus.InProgress))
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"A {Status} session cannot be resumed.");
        }

        if (mode == ResumeMode.RegenerateRemaining)
        {
            CancelPendingActivities();
        }

        Status = SessionStatus.InProgress;
        PausedAt = null;
        LastActivityAt = nowUtc;
        return mode;
    }

    /// <summary>
    /// IN_PROGRESS / PAUSED → COMPLETED. Unfinished planned activities are cancelled (hard stop). Idempotent when
    /// already completed (returns 0).
    /// </summary>
    public int Complete(DateTime nowUtc, SessionTimingRules rules)
    {
        if (Status == SessionStatus.Completed)
        {
            return 0;
        }

        EnsureUsable(nowUtc, rules);
        if (Status is not (SessionStatus.InProgress or SessionStatus.Paused))
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"A {Status} session cannot be completed.");
        }

        return End(SessionStatus.Completed, nowUtc, rules);
    }

    /// <summary>
    /// Explicit abandon (C5): IN_PROGRESS / PAUSED → ABANDONED. Attempts and evidence are kept.
    /// Idempotent when already abandoned (returns 0).
    /// </summary>
    public int Abandon(DateTime nowUtc, SessionTimingRules rules)
    {
        if (Status == SessionStatus.Abandoned)
        {
            return 0;
        }

        EnsureUsable(nowUtc, rules);
        if (!Status.IsOpen())
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"A {Status} session cannot be abandoned.");
        }

        return End(SessionStatus.Abandoned, nowUtc, rules);
    }

    /// <summary>
    /// SessionExpiryPolicy: an open session idle for longer than the expiry threshold becomes EXPIRED.
    /// No tail time is credited. Returns false when the session is not idle-expired.
    /// </summary>
    public bool ExpireIfIdle(DateTime nowUtc, SessionTimingRules rules)
    {
        if (!IsIdleExpired(nowUtc, rules))
        {
            return false;
        }

        CancelPendingActivities();
        Status = SessionStatus.Expired;
        PausedAt = null;
        CompletedAt = nowUtc;
        return true;
    }

    /// <summary>Cancels every pending activity (re-planning / regenerate remaining). Completed activities are untouched.</summary>
    public void CancelPendingActivities()
    {
        foreach (var activity in activities.Where(activity => activity.IsPending))
        {
            activity.Cancel();
        }
    }

    private DateTime IdleReference => LastActivityAt ?? GeneratedAt;

    private int End(SessionStatus finalStatus, DateTime nowUtc, SessionTimingRules rules)
    {
        var accrued = Status == SessionStatus.InProgress ? AccrueActiveTime(nowUtc, rules) : 0;
        CancelPendingActivities();
        Status = finalStatus;
        PausedAt = null;
        CompletedAt = nowUtc;
        return accrued;
    }

    private SessionActivity GetCurrentActivity(Guid activityId)
    {
        var activity = activities.SingleOrDefault(item => item.Id == activityId)
            ?? throw new DomainException(DomainErrorCodes.ActivityNotFound, "The activity does not belong to this session.");

        if (!activity.IsPending || CurrentActivity?.Id != activity.Id)
        {
            throw new DomainException(DomainErrorCodes.ActivityNotActive, "Only the current activity can be answered or completed.");
        }

        return activity;
    }

    private int AccrueActiveTime(DateTime nowUtc, SessionTimingRules rules)
    {
        var last = LastActivityAt ?? nowUtc;
        if (nowUtc < last)
        {
            throw new ArgumentException("Time cannot move backwards within a session.", nameof(nowUtc));
        }

        var gap = nowUtc - last;
        var credited = (int)(gap < rules.IdleCap ? gap : rules.IdleCap).TotalSeconds;
        ActualDurationSeconds += credited;
        LastActivityAt = nowUtc;
        return credited;
    }

    private void EnsureInProgress(DateTime nowUtc, SessionTimingRules rules)
    {
        EnsureUsable(nowUtc, rules);
        EnsureStatus(SessionStatus.InProgress);
    }

    private void EnsureStatus(SessionStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainException(DomainErrorCodes.SessionNotActive, $"The session is {Status}, expected {expected}.");
        }
    }

    /// <summary>Rejects commands on completed, expired (stored or idle-expired) and abandoned sessions.</summary>
    private void EnsureUsable(DateTime nowUtc, SessionTimingRules rules)
    {
        Guard.Utc(nowUtc);
        ArgumentNullException.ThrowIfNull(rules);

        switch (Status)
        {
            case SessionStatus.Completed:
                throw new DomainException(DomainErrorCodes.SessionAlreadyCompleted, "The session is already completed.");
            case SessionStatus.Expired:
                throw new DomainException(DomainErrorCodes.SessionExpired, "The session has expired.");
            case SessionStatus.Abandoned:
                throw new DomainException(DomainErrorCodes.SessionNotActive, "The session was abandoned.");
        }

        if (IsIdleExpired(nowUtc, rules))
        {
            throw new DomainException(DomainErrorCodes.SessionExpired, "The session has expired.");
        }
    }

    private static short? ToSeconds(int? seconds, string name) => seconds switch
    {
        null => null,
        <= 0 or > short.MaxValue => throw new ArgumentOutOfRangeException(name, seconds, "Duration must be a positive number of seconds."),
        _ => (short)seconds.Value,
    };
}

/// <summary>Result of <see cref="LearningSession.RecordAttempt"/>.</summary>
public sealed record AttemptRecorded(ActivityAttempt Attempt, bool ActivityClosed, int AccruedSeconds);

/// <summary>Result of <see cref="LearningSession.CompleteNonAssessmentActivity"/>.</summary>
public sealed record ActivityCompletion(bool AlreadyCompleted, int AccruedSeconds);
