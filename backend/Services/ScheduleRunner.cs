using Cronos;
using Ec2Manager.Api.Models;

namespace Ec2Manager.Api.Services;

public static class ScheduleRunner
{
    private static readonly Dictionary<string, DayOfWeek> DayMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mon"] = DayOfWeek.Monday, ["Tue"] = DayOfWeek.Tuesday, ["Wed"] = DayOfWeek.Wednesday,
        ["Thu"] = DayOfWeek.Thursday, ["Fri"] = DayOfWeek.Friday, ["Sat"] = DayOfWeek.Saturday, ["Sun"] = DayOfWeek.Sunday
    };

    /// <summary>
    /// Returns the next N fire times (UTC) for a schedule, starting search from 'from'.
    /// Bounded by ValidFrom/ValidTo.
    /// </summary>
    public static List<DateTimeOffset> GetNextFireTimes(Schedule s, DateTimeOffset from, int count)
    {
        var results = new List<DateTimeOffset>();
        var lowerBound = s.ValidFrom.HasValue && s.ValidFrom.Value > from ? s.ValidFrom.Value : from;

        if (!string.IsNullOrWhiteSpace(s.CronExpression))
        {
            var cron = CronExpression.Parse(s.CronExpression);
            var cursor = lowerBound.UtcDateTime;
            while (results.Count < count)
            {
                var next = cron.GetNextOccurrence(cursor, TimeZoneInfo.Utc);
                if (next is null) break;
                var nextOffset = new DateTimeOffset(next.Value, TimeSpan.Zero);
                if (s.ValidTo.HasValue && nextOffset > s.ValidTo.Value) break;
                results.Add(nextOffset);
                cursor = next.Value;
            }
            return results;
        }

        if (string.IsNullOrWhiteSpace(s.RecurrenceType) || s.RecurrenceType.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            // One-off: fires once at ValidFrom if it's in the future window
            if (s.ValidFrom.HasValue && s.ValidFrom.Value >= from)
            {
                if (!s.ValidTo.HasValue || s.ValidFrom.Value <= s.ValidTo.Value)
                    results.Add(s.ValidFrom.Value);
            }
            return results;
        }

        if (!s.TimeOfDay.HasValue) return results;

        var days = s.RecurrenceType.Equals("Daily", StringComparison.OrdinalIgnoreCase)
            ? null
            : (s.DaysOfWeekJson is null ? new List<DayOfWeek>() :
                System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.DaysOfWeekJson)!
                    .Select(d => DayMap[d]).ToList());

        var probe = lowerBound.UtcDateTime.Date;
        var guard = 0;
        while (results.Count < count && guard < 3650) // don't loop more than ~10 years out
        {
            guard++;
            var candidate = new DateTimeOffset(probe, TimeSpan.Zero) + s.TimeOfDay.Value;

            var dayMatches = days is null || days.Contains(probe.DayOfWeek);
            var afterLowerBound = candidate >= lowerBound;
            var withinValidTo = !s.ValidTo.HasValue || candidate <= s.ValidTo.Value;

            if (dayMatches && afterLowerBound)
            {
                if (!withinValidTo) break; // past the window, no point continuing
                results.Add(candidate);
            }

            probe = probe.AddDays(1);
        }

        return results;
    }

    public static bool ShouldFireNow(Schedule s, DateTimeOffset now, TimeSpan tolerance)
    {
        var upcoming = GetNextFireTimes(s, now - tolerance, 1);
        if (upcoming.Count == 0) return false;
        var fireTime = upcoming[0];
        return fireTime <= now && fireTime > now - tolerance;
    }
}
