using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.UpdateLearningGoal;

/// <summary>
/// Applies a learning goal change atomically under the user's lock (OB-G9): checks the account and the goal/plan state,
/// returns the active goal unchanged when it already has the desired values (no-op), otherwise deactivates it, creates
/// the new version (self-reported level copied) and rebases the active plan onto it. Inconsistent persisted state is a
/// server error (an exception), never repaired.
/// </summary>
public interface ILearningGoalStore
{
    Task<LearningGoalUpdateOutcome> UpdateAsync(LearningGoalChange change, CancellationToken cancellationToken);
}

/// <param name="UserId">Authenticated user.</param>
/// <param name="NewGoalId">Id of the new goal version if one is needed; fixed once per logical request.</param>
/// <param name="PrimaryGoal">Desired primary goal.</param>
/// <param name="TargetHskLevel">Desired target HSK level.</param>
/// <param name="TargetDate">Desired target date (null clears it).</param>
/// <param name="DailyStudyMinutes">Desired daily study minutes.</param>
/// <param name="NowUtc">Time of the operation.</param>
public sealed record LearningGoalChange(
    Guid UserId,
    Guid NewGoalId,
    PrimaryGoal PrimaryGoal,
    byte? TargetHskLevel,
    DateOnly? TargetDate,
    short DailyStudyMinutes,
    DateTime NowUtc);

public enum LearningGoalUpdateStatus
{
    /// <summary>The active goal now has the desired values (a new version was created, or it already had them).</summary>
    Applied,

    /// <summary>User missing or not ACTIVE. Nothing was written.</summary>
    AccountDisabled,

    /// <summary>No active plan and no active goal: onboarding is not completed. Nothing was written.</summary>
    LearningPlanNotFound,
}

public sealed record LearningGoalUpdateOutcome(LearningGoalUpdateStatus Status, UpdateLearningGoalResult? ActiveGoal)
{
    public static LearningGoalUpdateOutcome Applied(UpdateLearningGoalResult activeGoal) => new(LearningGoalUpdateStatus.Applied, activeGoal);

    public static LearningGoalUpdateOutcome Rejected(LearningGoalUpdateStatus status) =>
        status == LearningGoalUpdateStatus.Applied
            ? throw new ArgumentOutOfRangeException(nameof(status), status, "An applied update carries the active goal.")
            : new(status, null);
}
