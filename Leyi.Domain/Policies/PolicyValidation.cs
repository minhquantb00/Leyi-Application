namespace Leyi.Domain.Policies;

/// <summary>Consistency checks shared by the policy configuration types.</summary>
internal static class PolicyValidation
{
    private const decimal SumTolerance = 0.0001m;

    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Invalid policy configuration: {message}");
        }
    }

    public static void RequireVersion(string? version) =>
        Require(!string.IsNullOrWhiteSpace(version), "a policy version is required.");

    public static void RequireSumIsOne(IEnumerable<decimal> values, string name) =>
        Require(Math.Abs(values.Sum() - 1m) <= SumTolerance, $"{name} must sum to 1.");

    public static void RequireEveryValue<TEnum>(IEnumerable<TEnum> keys, string name)
        where TEnum : struct, Enum =>
        Require(keys.ToHashSet().SetEquals(Enum.GetValues<TEnum>()), $"{name} must define every {typeof(TEnum).Name}.");

    public static bool IsScore(decimal value) => value is >= 0m and <= 100m;

    public static bool IsFraction(decimal value) => value is >= 0m and <= 1m;
}
