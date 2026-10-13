using Leyi.Domain.Common;
using Leyi.Domain.Learning;
using Leyi.Domain.LearningProfile;

namespace Leyi.Domain.Policies;

/// <summary>
/// Goal modifier configuration (ASE §17, §19, §52; E1): a light modifier per (primary goal, dimension) within
/// [<see cref="MinModifier"/>, <see cref="MaxModifier"/>] that multiplies the FinalScore of an assessment candidate by
/// the modifier of its primary dimension. It never overrides SRS. A non-assessment activity (no primary dimension, e.g.
/// NEW_VOCABULARY_EXPOSURE) gets <see cref="MinModifier"/>; <c>TargetHskLevel</c> adds no modifier in V1.
/// Values in <see cref="V1"/> are V1 defaults (N1).
/// </summary>
public sealed record GoalPolicy
{
    public static GoalPolicy V1 { get; } = new()
    {
        Version = "goal-v1.1",
        MinModifier = 1.00m,
        MaxModifier = 1.20m,
        Modifiers = new Dictionary<PrimaryGoal, IReadOnlyDictionary<MasteryDimension, decimal>>
        {
            [PrimaryGoal.Hsk] = Row(recognition: 1.20m, recall: 1.20m, listening: 1.15m, usage: 1.10m),
            [PrimaryGoal.Communication] = Row(recognition: 1.00m, recall: 1.05m, listening: 1.20m, usage: 1.20m),
            [PrimaryGoal.Work] = Row(recognition: 1.05m, recall: 1.10m, listening: 1.15m, usage: 1.20m),
            [PrimaryGoal.Travel] = Row(recognition: 1.00m, recall: 1.05m, listening: 1.20m, usage: 1.20m),
            [PrimaryGoal.Study] = Row(recognition: 1.15m, recall: 1.20m, listening: 1.10m, usage: 1.15m),
            [PrimaryGoal.Interpretation] = Row(recognition: 1.05m, recall: 1.20m, listening: 1.20m, usage: 1.15m),
            [PrimaryGoal.LivingInChina] = Row(recognition: 1.00m, recall: 1.10m, listening: 1.20m, usage: 1.20m),
            [PrimaryGoal.Other] = Row(recognition: 1.00m, recall: 1.00m, listening: 1.00m, usage: 1.00m),
        },
    };

    public required string Version { get; init; }

    public required decimal MinModifier { get; init; }

    public required decimal MaxModifier { get; init; }

    /// <summary>Modifier per primary goal and dimension; every goal and every dimension must be present.</summary>
    public required IReadOnlyDictionary<PrimaryGoal, IReadOnlyDictionary<MasteryDimension, decimal>> Modifiers { get; init; }

    /// <summary>
    /// The modifier of <paramref name="primaryDimension"/> for <paramref name="primaryGoal"/>; a non-assessment activity
    /// (<paramref name="primaryDimension"/> null) is neutral. An unknown goal or dimension is an error, never a default.
    /// </summary>
    public decimal ModifierFor(PrimaryGoal primaryGoal, MasteryDimension? primaryDimension)
    {
        Guard.Defined(primaryGoal);
        if (primaryDimension is not { } dimension)
        {
            return MinModifier;
        }

        return Modifiers.TryGetValue(primaryGoal, out var row) && row.TryGetValue(Guard.Defined(dimension), out var modifier)
            ? modifier
            : throw new InvalidOperationException($"Goal policy {Version} defines no modifier for {primaryGoal} / {dimension}.");
    }

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(MinModifier == 1.00m, "Goal modifiers never reduce priority (minimum 1.00).");
        PolicyValidation.Require(MaxModifier >= MinModifier, "The maximum goal modifier must not be below the minimum.");
        PolicyValidation.RequireEveryValue(Modifiers.Keys, nameof(Modifiers));

        foreach (var (goal, row) in Modifiers)
        {
            PolicyValidation.RequireEveryValue(row.Keys, $"{nameof(Modifiers)} of {goal}");
            PolicyValidation.Require(
                row.Values.All(modifier => modifier >= MinModifier && modifier <= MaxModifier),
                $"Goal modifiers of {goal} must be within {MinModifier}–{MaxModifier}.");
        }
    }

    private static IReadOnlyDictionary<MasteryDimension, decimal> Row(decimal recognition, decimal recall, decimal listening, decimal usage) =>
        new Dictionary<MasteryDimension, decimal>
        {
            [MasteryDimension.Recognition] = recognition,
            [MasteryDimension.Recall] = recall,
            [MasteryDimension.Listening] = listening,
            [MasteryDimension.Usage] = usage,
        };
}
