using FluentValidation;

namespace Leyi.Application.Features.Onboarding.UpdateLearningGoal;

/// <summary>Same goal rules as onboarding (OB-G5): no future-date rule for the target date.</summary>
internal sealed class UpdateLearningGoalValidator : AbstractValidator<UpdateLearningGoalCommand>
{
    public UpdateLearningGoalValidator()
    {
        RuleFor(command => command.PrimaryGoal).MustBePrimaryGoal();

        RuleFor(command => command.TargetHskLevel)
            .MustBePresentForHskGoal()
            .When(command => LearningGoalRules.IsHskGoal(command.PrimaryGoal));

        RuleFor(command => command.TargetHskLevel)
            .MustBeHskLevel()
            .When(command => command.TargetHskLevel is not null);

        RuleFor(command => command.DailyStudyMinutes).MustBeDailyStudyMinutes();
    }
}
