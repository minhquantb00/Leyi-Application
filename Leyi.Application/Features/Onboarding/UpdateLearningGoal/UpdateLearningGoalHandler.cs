using FluentValidation;
using Leyi.Application.Abstractions.Messaging;
using Leyi.Application.Abstractions.Results;
using Leyi.Application.Features.Onboarding.CompleteOnboarding;

namespace Leyi.Application.Features.Onboarding.UpdateLearningGoal;

/// <summary>
/// Changes the learner's goal (UpdateLearningGoal.md, OB-G9): primary goal, target HSK, target date, daily minutes. The
/// self-reported level, curriculum position, plan type, placement, mastery, SRS and any open session are not touched;
/// a new goal applies from the next session. No integration event is published.
/// </summary>
internal sealed class UpdateLearningGoalHandler : ICommandHandler<UpdateLearningGoalCommand, Result<UpdateLearningGoalResult>>
{
    private readonly IValidator<UpdateLearningGoalCommand> validator;
    private readonly ILearningGoalStore learningGoalStore;
    private readonly TimeProvider timeProvider;

    public UpdateLearningGoalHandler(IValidator<UpdateLearningGoalCommand> validator, ILearningGoalStore learningGoalStore, TimeProvider timeProvider)
    {
        this.validator = validator;
        this.learningGoalStore = learningGoalStore;
        this.timeProvider = timeProvider;
    }

    public async Task<Result<UpdateLearningGoalResult>> HandleAsync(UpdateLearningGoalCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            throw new ArgumentException("The authenticated user id is required.", nameof(command));
        }

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            return LearningGoalRules.ToError(validation);
        }

        OnboardingWireValues.TryParsePrimaryGoal(command.PrimaryGoal, out var primaryGoal);

        // The id of a possible new goal version is fixed here, once per logical request: the store uses it to recognize
        // its own commit after a connection failure.
        var change = new LearningGoalChange(
            command.UserId,
            Guid.CreateVersion7(),
            primaryGoal,
            command.TargetHskLevel is { } targetHskLevel ? (byte)targetHskLevel : null,
            command.TargetDate,
            (short)command.DailyStudyMinutes!.Value,
            timeProvider.GetUtcNow().UtcDateTime);

        var outcome = await learningGoalStore.UpdateAsync(change, cancellationToken);

        return outcome.Status switch
        {
            LearningGoalUpdateStatus.Applied when outcome.ActiveGoal is { } activeGoal => activeGoal,
            LearningGoalUpdateStatus.AccountDisabled => OnboardingErrors.AccountDisabled,
            LearningGoalUpdateStatus.LearningPlanNotFound => OnboardingErrors.LearningPlanNotFound,
            _ => throw new InvalidOperationException($"Unexpected learning goal update outcome {outcome.Status}."),
        };
    }
}
