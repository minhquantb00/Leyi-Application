using Leyi.Domain.Common;
using Leyi.Domain.Knowledge;

namespace Leyi.Domain.LearningProfile;

/// <summary>
/// A learner's goal (ERD §6.2). History is kept: changing the goal deactivates the old record and creates a new one.
/// At most one active goal per user (filtered unique index). Changing a goal never resets mastery or evidence.
/// </summary>
public sealed class UserLearningGoal : Entity
{
    /// <summary>Daily goal bounds (C8): presets 10/15/20/30/45/60 or custom 5–120 in steps of 5.</summary>
    public const short MinDailyStudyMinutes = 5;

    public const short MaxDailyStudyMinutes = 120;

    public const short DailyStudyMinutesStep = 5;

    private UserLearningGoal()
    {
    }

    private UserLearningGoal(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public PrimaryGoal PrimaryGoal { get; private set; }

    public SelfReportedLevel SelfReportedLevel { get; private set; }

    public byte? TargetHskLevel { get; private set; }

    public DateOnly? TargetDate { get; private set; }

    /// <summary>Daily goal; distinct from the quick-session duration (3/5/7/10/15/20).</summary>
    public short DailyStudyMinutes { get; private set; }

    public bool IsActive { get; private set; }

    public static bool IsValidDailyStudyMinutes(int minutes) =>
        minutes is >= MinDailyStudyMinutes and <= MaxDailyStudyMinutes && minutes % DailyStudyMinutesStep == 0;

    public static UserLearningGoal Create(
        Guid userId,
        PrimaryGoal primaryGoal,
        SelfReportedLevel selfReportedLevel,
        short dailyStudyMinutes,
        byte? targetHskLevel = null,
        DateOnly? targetDate = null)
    {
        if (!IsValidDailyStudyMinutes(dailyStudyMinutes))
        {
            throw new DomainException(DomainErrorCodes.DailyStudyMinutesInvalid, $"Daily study minutes must be {MinDailyStudyMinutes}–{MaxDailyStudyMinutes} in steps of {DailyStudyMinutesStep}.");
        }

        if (targetHskLevel is not null && !HskLevels.IsValid(targetHskLevel.Value))
        {
            throw new DomainException(DomainErrorCodes.TargetHskInvalid, $"Target HSK level must be between {HskLevels.Min} and {HskLevels.Max}.");
        }

        return new UserLearningGoal(NewId())
        {
            UserId = Guard.NotEmpty(userId),
            PrimaryGoal = Guard.Defined(primaryGoal),
            SelfReportedLevel = Guard.Defined(selfReportedLevel),
            DailyStudyMinutes = dailyStudyMinutes,
            TargetHskLevel = targetHskLevel,
            TargetDate = targetDate,
            IsActive = true,
        };
    }

    public void Deactivate() => IsActive = false;
}
