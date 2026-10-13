using Leyi.Domain.Common;

namespace Leyi.Domain.Learning;

/// <summary>
/// The learner's local day (Domain Model §13): UTC instant + IANA timezone → local date and the UTC instant at which the
/// next local day starts (<c>endOfUserLocalDay</c>). The single source for DueToday, DailyLearningStats.LocalDate and
/// evidence local dates. Deterministic and free of I/O; never uses the server's timezone.
/// </summary>
public static class LearnerLocalDateResolver
{
    /// <summary>Upper bound for skipping a clock gap at local midnight (real gaps are at most a few hours).</summary>
    private static readonly TimeSpan MaxClockGap = TimeSpan.FromDays(1);

    private static readonly TimeSpan GapStep = TimeSpan.FromMinutes(1);

    /// <summary>Finds a timezone by IANA id; Windows-only ids are not accepted.</summary>
    public static bool TryFindTimeZone(string? ianaId, out TimeZoneInfo timeZone)
    {
        if (!string.IsNullOrWhiteSpace(ianaId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaId, out var found)
            && found.HasIanaId)
        {
            timeZone = found;
            return true;
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }

    public static LearnerLocalDay Resolve(DateTime nowUtc, TimeZoneInfo timeZone)
    {
        Guard.Utc(nowUtc);
        ArgumentNullException.ThrowIfNull(timeZone);

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone));
        return new LearnerLocalDay(localDate, StartOfLocalDayUtc(localDate.AddDays(1), timeZone));
    }

    /// <summary>
    /// First instant of <paramref name="date"/> in <paramref name="timeZone"/>. Midnight inside a DST gap → the first
    /// valid local time after the gap; ambiguous midnight → its first occurrence (the larger offset).
    /// </summary>
    private static DateTime StartOfLocalDayUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var local = midnight;
        while (timeZone.IsInvalidTime(local))
        {
            local += GapStep;
            if (local - midnight > MaxClockGap)
            {
                throw new InvalidOperationException($"No valid local time on {date} in timezone {timeZone.Id}.");
            }
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            var firstOccurrenceOffset = timeZone.GetAmbiguousTimeOffsets(local).Max();
            return DateTime.SpecifyKind(local - firstOccurrenceOffset, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, timeZone);
    }
}

/// <param name="LocalDate">The learner's current local date.</param>
/// <param name="NextLocalDayStartUtc">UTC instant at which the next local day starts (exclusive end of today).</param>
public sealed record LearnerLocalDay(DateOnly LocalDate, DateTime NextLocalDayStartUtc);
