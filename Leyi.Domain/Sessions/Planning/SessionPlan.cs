using Leyi.Domain.Common;
using Leyi.Domain.Learning;
using Leyi.Domain.Policies;
using Leyi.Domain.Sessions.Scoring;

namespace Leyi.Domain.Sessions.Planning;

/// <summary>Inputs of one planning call besides the candidates: session duration and the learner's cold-start phase and review-debt level.</summary>
public sealed record SessionPlanRequest
{
    public SessionPlanRequest(int durationMinutes, ColdStartPhase phase, ReviewDebtLevel debtLevel)
    {
        DurationMinutes = Guard.Positive(durationMinutes);
        Phase = Guard.Defined(phase);
        DebtLevel = Guard.Defined(debtLevel);
    }

    public int DurationMinutes { get; }

    public ColdStartPhase Phase { get; }

    public ReviewDebtLevel DebtLevel { get; }
}

/// <summary>Role of a planned activity: an ordinary assessment, or one half of a NEW vocabulary bundle (AS-G7).</summary>
public enum PlannedActivityRole
{
    Assessment,
    NewExposure,
    NewRecall,
}

/// <summary>
/// One activity of the plan, in final order. Content-free: ActivityContentFactory turns it into a snapshot without
/// reordering. A NEW exposure has no dimension and no score, and its only reason is CURRICULUM without contribution
/// (AS-G20); its paired recall (same <see cref="BundleId"/>) carries the bundle's score and scored reasons.
/// </summary>
/// <param name="SortOrder">
/// 0-based position in the plan. Planner-internal: persistence appends the activities in this order through
/// <c>LearningSession.AddActivity</c>, which assigns the persisted SortOrder itself; never store this value directly.
/// </param>
/// <param name="Reasons">Selection reasons to persist (at least one): the score contributions of an assessment, CURRICULUM for an exposure.</param>
/// <param name="BundleId">Shared by the two activities of a NEW bundle (its knowledge unit id); null for an assessment.</param>
public sealed record PlannedActivity(
    int SortOrder,
    Guid KnowledgeUnitId,
    ActivityType ActivityType,
    MasteryDimension? PrimaryDimension,
    PlannedActivityRole Role,
    int ContentVersion,
    int EstimatedDurationSeconds,
    CandidateScore? Score,
    IReadOnlyList<ActivitySelectionReason> Reasons,
    Guid? BundleId);

/// <summary>
/// Planning figures kept for observability (CLAUDE §21), computed from the final selection; never read back by the
/// planner. <c>X1Applied</c> = the final NEW seconds exceed the ordinary cap; <c>WeakTargetSeconds</c> is 0 when the
/// learner has no weak skill dimension.
/// </summary>
public sealed record SessionPlanDiagnostics(
    ColdStartPhase Phase,
    ReviewDebtLevel DebtLevel,
    int EffectiveBudgetSeconds,
    int NewCapSeconds,
    int NewUsedSeconds,
    int WeakTargetSeconds,
    int WeakUsedSeconds,
    bool CoreReviewSelected,
    bool X1Applied,
    int? FallbackStep,
    int DistinctAssessmentTypes,
    int AssessmentTypeTarget);

/// <summary>The selected and ordered activities of a session. <see cref="EstimatedDurationSeconds"/> is the sum of the activities.</summary>
public sealed record SessionPlan(IReadOnlyList<PlannedActivity> Activities, int EstimatedDurationSeconds, SessionPlanDiagnostics Diagnostics);

public enum SessionPlanOutcome
{
    PlanCreated,

    /// <summary>Neither the normal plan nor the fallback ladder found an activity (mapped to SESSION_NO_ELIGIBLE_CONTENT by the application).</summary>
    NoEligibleContent,
}

public sealed record SessionPlanResult(SessionPlanOutcome Outcome, SessionPlan? Plan)
{
    public static SessionPlanResult Created(SessionPlan plan) => new(SessionPlanOutcome.PlanCreated, plan ?? throw new ArgumentNullException(nameof(plan)));

    public static SessionPlanResult NoEligibleContent { get; } = new(SessionPlanOutcome.NoEligibleContent, null);
}
