namespace Leyi.Domain.Policies;

/// <summary>
/// Goal modifier configuration (ASE §17, §52): a light dimension-based modifier within [<see cref="MinModifier"/>,
/// <see cref="MaxModifier"/>] that never overrides SRS. The per-goal, per-dimension modifier values are not specified
/// in the V1 documents yet, so this policy only fixes the documented bounds.
/// </summary>
public sealed record GoalPolicy
{
    public static GoalPolicy V1 { get; } = new()
    {
        Version = "goal-v1.0",
        MinModifier = 1.00m,
        MaxModifier = 1.20m,
    };

    public required string Version { get; init; }

    public required decimal MinModifier { get; init; }

    public required decimal MaxModifier { get; init; }

    public void Validate()
    {
        PolicyValidation.RequireVersion(Version);
        PolicyValidation.Require(MinModifier == 1.00m, "Goal modifiers never reduce priority (minimum 1.00).");
        PolicyValidation.Require(MaxModifier >= MinModifier, "The maximum goal modifier must not be below the minimum.");
    }
}
