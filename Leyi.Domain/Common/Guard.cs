using System.Runtime.CompilerServices;

namespace Leyi.Domain.Common;

/// <summary>
/// Argument checks for programming errors. Business rule violations throw <see cref="DomainException"/>.
/// </summary>
internal static class Guard
{
    public static Guid NotEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value == Guid.Empty ? throw new ArgumentException("Identifier must not be empty.", name) : value;

    public static string NotBlank(string? value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();

    public static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static TEnum Defined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))] string? name = null)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(name, value, $"Undefined {typeof(TEnum).Name}.");

    public static DateTime Utc(DateTime value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);

    public static DateTime? Utc(DateTime? value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value is null ? null : Utc(value.Value, name);

    public static int NotNegative(int value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value < 0 ? throw new ArgumentOutOfRangeException(name, value, "Value must not be negative.") : value;

    public static int Positive(int value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value <= 0 ? throw new ArgumentOutOfRangeException(name, value, "Value must be positive.") : value;

    public static decimal InRange(decimal value, decimal min, decimal max, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value < min || value > max
            ? throw new ArgumentOutOfRangeException(name, value, $"Value must be between {min} and {max}.")
            : value;

    public static TimeSpan Positive(TimeSpan value, [CallerArgumentExpression(nameof(value))] string? name = null) =>
        value <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(name, value, "Duration must be positive.") : value;
}
