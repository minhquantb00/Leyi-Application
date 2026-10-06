namespace Leyi.Domain.Knowledge;

/// <summary>HSK level range used by vocabulary, grammar and learning goals (ERD: 1–6).</summary>
public static class HskLevels
{
    public const byte Min = 1;

    public const byte Max = 6;

    public static bool IsValid(byte level) => level is >= Min and <= Max;

    internal static byte? Validate(byte? level, string parameterName) =>
        level is null || IsValid(level.Value)
            ? level
            : throw new ArgumentOutOfRangeException(parameterName, level, $"HSK level must be between {Min} and {Max}.");
}
