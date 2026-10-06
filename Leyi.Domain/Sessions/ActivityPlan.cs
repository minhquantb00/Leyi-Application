using Leyi.Domain.Learning;

namespace Leyi.Domain.Sessions;

/// <summary>One planned activity produced by the SessionPlanner, added through <see cref="LearningSession.AddActivity"/>.</summary>
public sealed record ActivityPlan
{
    public required Guid KnowledgeUnitId { get; init; }

    public required ActivityType ActivityType { get; init; }

    /// <summary>Primary dimension; required for assessment activities, null for NEW_VOCABULARY_EXPOSURE.</summary>
    public MasteryDimension? TargetDimension { get; init; }

    public required int EstimatedDurationSeconds { get; init; }

    /// <summary>Base PriorityScore 0–100 at planning time (modifiers are stored as reasons).</summary>
    public decimal? PriorityScore { get; init; }

    /// <summary>Opaque, versioned JSON content snapshot (prompt, options, accepted answers). Never sent with answers.</summary>
    public required string ContentSnapshot { get; init; }

    public required int ContentVersion { get; init; }

    public required IReadOnlyCollection<ActivitySelectionReason> Reasons { get; init; }
}

public sealed record ActivitySelectionReason(SelectionReason Reason, decimal? ScoreContribution = null);
