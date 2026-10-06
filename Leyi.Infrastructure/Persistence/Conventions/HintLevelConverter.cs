using System.Collections.Frozen;
using Leyi.Domain.Learning;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Leyi.Infrastructure.Persistence.Conventions;

/// <summary>
/// Stores <see cref="HintLevel"/> as the documented values NONE / LEVEL_1 / LEVEL_2 (ERD §6.21).
/// The generic upper-snake converter would produce LEVEL1, so the mapping is explicit.
/// </summary>
internal sealed class HintLevelConverter : ValueConverter<HintLevel, string>
{
    private static readonly FrozenDictionary<HintLevel, string> ToStore = new Dictionary<HintLevel, string>
    {
        [HintLevel.None] = "NONE",
        [HintLevel.Level1] = "LEVEL_1",
        [HintLevel.Level2] = "LEVEL_2",
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, HintLevel> FromStore =
        ToStore.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public HintLevelConverter()
        : base(value => Convert(value), value => Parse(value))
    {
    }

    private static string Convert(HintLevel value) =>
        ToStore.TryGetValue(value, out var stored) ? stored : throw new InvalidOperationException($"'{value}' is not a defined hint level.");

    private static HintLevel Parse(string value) =>
        FromStore.TryGetValue(value, out var parsed) ? parsed : throw new InvalidOperationException($"'{value}' is not a known hint level.");
}
