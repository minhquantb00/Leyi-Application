using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;

namespace Leyi.Application.Features.Onboarding.UpdateLearningGoal;

/// <summary>
/// Full desired state of the learner-editable goal fields (UpdateLearningGoal.md, OB-G9). <paramref name="UserId"/> comes
/// from the authenticated principal. A null <paramref name="TargetDate"/> means "no target date". The self-reported
/// level is not editable and is not part of the command.
/// </summary>
public sealed record UpdateLearningGoalCommand(
    Guid UserId,
    string? PrimaryGoal,
    int? TargetHskLevel,
    DateOnly? TargetDate,
    int? DailyStudyMinutes) : ICommand<Result<UpdateLearningGoalResult>>;
