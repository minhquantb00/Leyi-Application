using System.Collections.Frozen;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Leyi.Infrastructure.Persistence.Conventions;

/// <summary>
/// Stores enum members as UPPER_SNAKE_CASE strings (e.g. <c>InProgress</c> → <c>IN_PROGRESS</c>),
/// matching the value lists documented in the ERD.
/// </summary>
internal sealed class UpperSnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    private static readonly FrozenDictionary<TEnum, string> ToStore =
        Enum.GetValues<TEnum>().ToFrozenDictionary(value => value, value => ToUpperSnakeCase(value.ToString()));

    private static readonly FrozenDictionary<string, TEnum> FromStore =
        ToStore.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public UpperSnakeCaseEnumConverter()
        : base(value => Convert(value), value => Parse(value))
    {
    }

    private static string Convert(TEnum value) =>
        ToStore.TryGetValue(value, out var stored)
            ? stored
            : throw new InvalidOperationException($"'{value}' is not a defined {typeof(TEnum).Name} value.");

    private static TEnum Parse(string value) =>
        FromStore.TryGetValue(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"'{value}' is not a known {typeof(TEnum).Name} value.");

    private static string ToUpperSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);
        for (var index = 0; index < name.Length; index++)
        {
            var character = name[index];
            if (index > 0 && char.IsUpper(character))
            {
                builder.Append('_');
            }

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
