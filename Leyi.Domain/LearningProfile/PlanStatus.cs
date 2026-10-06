namespace Leyi.Domain.LearningProfile;

/// <summary>Learning plan status (ERD §6.3). At most one ACTIVE plan per user (application invariant).</summary>
public enum PlanStatus
{
    Active,
    Paused,
    Completed,
    Archived,
}
