namespace Leyi.Domain.Common;

/// <summary>
/// Error codes raised by domain invariants. Values are defined in <c>0.Document/05.API/ErrorCodeCatalog.md</c>.
/// </summary>
public static class DomainErrorCodes
{
    public const string Conflict = "CONFLICT";

    public const string SessionNotActive = "SESSION_NOT_ACTIVE";
    public const string SessionAlreadyCompleted = "SESSION_ALREADY_COMPLETED";
    public const string SessionExpired = "SESSION_EXPIRED";

    public const string ActivityNotFound = "ACTIVITY_NOT_FOUND";
    public const string ActivityNotActive = "ACTIVITY_NOT_ACTIVE";
    public const string ActivityTypeNotSupported = "ACTIVITY_TYPE_NOT_SUPPORTED";

    public const string MasteryStateInvalid = "MASTERY_STATE_INVALID";
    public const string EvidenceInvalid = "EVIDENCE_INVALID";
    public const string ReviewLevelInvalid = "REVIEW_LEVEL_INVALID";

    public const string LearningPlanInactive = "LEARNING_PLAN_INACTIVE";
    public const string DailyStudyMinutesInvalid = "DAILY_STUDY_MINUTES_INVALID";
    public const string TargetHskInvalid = "TARGET_HSK_INVALID";
}
