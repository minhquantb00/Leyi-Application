using Leyi.Domain.Common;

namespace Leyi.Domain.Mastery;

/// <summary>
/// SRS review level 0–7 (Learning Model §22). Level 0 is the 10-minute reinforcement level right after learning;
/// it never counts toward ReviewDebt. Intervals per level live in <see cref="Policies.SrsPolicy"/>.
/// </summary>
public readonly record struct ReviewLevel
{
    public const byte MinValue = 0;

    public const byte MaxValue = 7;

    private ReviewLevel(byte value)
    {
        Value = value;
    }

    public static ReviewLevel Initial => new(MinValue);

    public static ReviewLevel Highest => new(MaxValue);

    public byte Value { get; }

    /// <summary>Level ≥ 1: the item is in the SRS review cycle and counts toward ReviewDebt when due.</summary>
    public bool IsInReviewCycle => Value >= 1;

    public static ReviewLevel From(int value) =>
        value is < MinValue or > MaxValue
            ? throw new DomainException(DomainErrorCodes.ReviewLevelInvalid, $"Review level must be between {MinValue} and {MaxValue}.")
            : new ReviewLevel((byte)value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
