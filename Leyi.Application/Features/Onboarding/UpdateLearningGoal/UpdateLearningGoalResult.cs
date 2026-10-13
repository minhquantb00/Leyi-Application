using Leyi.Domain.LearningProfile;

namespace Leyi.Application.Features.Onboarding.UpdateLearningGoal;

/// <summary>The active learning goal after the update (or unchanged after a no-op). No ids are exposed.</summary>
public sealed record UpdateLearningGoalResult(
    PrimaryGoal PrimaryGoal,
    SelfReportedLevel SelfReportedLevel,
    byte? TargetHskLevel,
    DateOnly? TargetDate,
    short DailyStudyMinutes)
{
    public static UpdateLearningGoalResult From(UserLearningGoal goal)
    {
        ArgumentNullException.ThrowIfNull(goal);
        return new(goal.PrimaryGoal, goal.SelfReportedLevel, goal.TargetHskLevel, goal.TargetDate, goal.DailyStudyMinutes);
    }
}
