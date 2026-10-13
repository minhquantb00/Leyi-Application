using Leyi.Domain.Common;

namespace Leyi.Domain.LearningProfile;

/// <summary>
/// Aggregate root: a learner's current path through a curriculum (Domain Model §5, ERD §6.3).
/// Completing or archiving a plan never removes mastery or review schedules.
/// "Only one ACTIVE plan per user" and "CurrentLesson belongs to CurrentLevel" need cross-aggregate data and are
/// enforced by the use cases that change plans.
/// </summary>
public sealed class UserLearningPlan : Entity
{
    private UserLearningPlan()
    {
    }

    private UserLearningPlan(Guid id)
        : base(id)
    {
    }

    public Guid UserId { get; private set; }

    public Guid? GoalId { get; private set; }

    public Guid CurriculumId { get; private set; }

    public Guid? CurrentLevelId { get; private set; }

    public Guid? CurrentLessonId { get; private set; }

    public PlanType PlanType { get; private set; }

    public PlanStatus Status { get; private set; }

    public PlacementStatus PlacementStatus { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateOnly? TargetDate { get; private set; }

    public static UserLearningPlan Start(
        Guid userId,
        Guid curriculumId,
        PlanType planType,
        PlacementStatus placementStatus,
        DateTime nowUtc,
        Guid? goalId = null,
        Guid? currentLevelId = null,
        Guid? currentLessonId = null,
        DateOnly? targetDate = null)
    {
        if (currentLessonId is not null && currentLevelId is null)
        {
            throw new ArgumentException("A current lesson requires its current level.", nameof(currentLevelId));
        }

        return new UserLearningPlan(NewId())
        {
            UserId = Guard.NotEmpty(userId),
            CurriculumId = Guard.NotEmpty(curriculumId),
            PlanType = Guard.Defined(planType),
            PlacementStatus = Guard.Defined(placementStatus),
            Status = PlanStatus.Active,
            StartedAt = Guard.Utc(nowUtc),
            GoalId = goalId,
            CurrentLevelId = currentLevelId,
            CurrentLessonId = currentLessonId,
            TargetDate = targetDate,
        };
    }

    public void ChangeCurrentLesson(Guid currentLevelId, Guid currentLessonId)
    {
        EnsureActive();
        CurrentLevelId = Guard.NotEmpty(currentLevelId);
        CurrentLessonId = Guard.NotEmpty(currentLessonId);
    }

    /// <summary>
    /// The learner's active goal changed (UpdateLearningGoal, OB-G9): the plan follows the new active goal and keeps
    /// its target date equal to the goal's (also when it is cleared). The curriculum position, plan type, placement
    /// status and start time are not changed — a goal change never remaps the curriculum.
    /// </summary>
    public void RebaseToGoal(Guid goalId, DateOnly? targetDate)
    {
        EnsureActive();
        GoalId = Guard.NotEmpty(goalId);
        TargetDate = targetDate;
    }

    public void ChangePlacementStatus(PlacementStatus placementStatus) => PlacementStatus = Guard.Defined(placementStatus);

    public void Pause()
    {
        EnsureActive();
        Status = PlanStatus.Paused;
    }

    public void Resume()
    {
        if (Status != PlanStatus.Paused)
        {
            throw new DomainException(DomainErrorCodes.LearningPlanInactive, $"Only a paused plan can resume; status is {Status}.");
        }

        Status = PlanStatus.Active;
    }

    public void Complete()
    {
        EnsureActive();
        Status = PlanStatus.Completed;
    }

    public void Archive() => Status = PlanStatus.Archived;

    private void EnsureActive()
    {
        if (Status != PlanStatus.Active)
        {
            throw new DomainException(DomainErrorCodes.LearningPlanInactive, $"The learning plan is {Status}.");
        }
    }
}
