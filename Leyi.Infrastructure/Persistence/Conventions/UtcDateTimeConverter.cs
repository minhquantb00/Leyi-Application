using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Leyi.Infrastructure.Persistence.Conventions;

/// <summary>
/// All persisted timestamps are UTC. Values written with <see cref="DateTimeKind.Local"/> are converted to UTC,
/// <see cref="DateTimeKind.Unspecified"/> values are treated as UTC, and values read back are marked as UTC.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(value => ToUtc(value), value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
