using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain.Enums.EnumExtensions;

namespace Domain.Helper
{
    // Computational core of the "Nth weekday of month" week-off pattern
    // (e.g. "2nd and 4th Saturday of every month"), additive alongside the
    // original "this weekday every week" pattern
    // (Domain.Entities.WeekOff / WeekOffPatternType.EveryWeek).
    //
    // Consumed by Application/Services/Attendances/AttendanceService.cs'
    // reporting/dashboard methods (GetCalendarAsync, GetTeamAttendanceAsync,
    // GetSummaryAsync, GetDashboardAsync) via the same
    // AttendanceStatusHelper.ClassifyDay() overlay those methods already
    // used for the legacy fixed-weekday WeekOff data - this is the same
    // read path, extended to understand the new pattern type, not a new
    // parallel one. Never consulted by the live punch-processing path
    // (PunchInCoreAsync/PunchOutCoreAsync), which does not consult WeekOff
    // at all today (see Phase1-Existing-System-Audit.md).
    public static class WeekOffCalculator
    {
        // Nth occurrence of `weekday` within `year`/`month` (1 = first,
        // ... 5 = fifth). Returns null when that occurrence does not exist
        // in that month (e.g. "5th Saturday" in a month that only has 4) -
        // never throws.
        public static DateTime? GetNthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int occurrence)
        {
            if (occurrence < 1 || occurrence > 5)
                return null;

            var firstOfMonth = new DateTime(year, month, 1);

            // Days from the 1st to the first occurrence of `weekday`.
            int offset = ((int)weekday - (int)firstOfMonth.DayOfWeek + 7) % 7;
            var firstOccurrence = firstOfMonth.AddDays(offset);

            var target = firstOccurrence.AddDays((occurrence - 1) * 7);

            // Guard against spilling into the next month (e.g. requesting
            // the 5th occurrence when the month only has 4).
            return target.Month == month && target.Year == year ? target : (DateTime?)null;
        }

        // All calendar dates in `year`/`month` that are a week-off per the
        // given WeekOff configurations (both patterns, for one
        // tenant/branch - callers filter `configs` to the right scope
        // beforehand). Gracefully returns an empty set for null/empty
        // config lists; never throws for a month with fewer occurrences of
        // a weekday than configured.
        public static HashSet<DateTime> GetWeekOffDatesForMonth(int year, int month, IEnumerable<WeekOff>? configs)
        {
            var result = new HashSet<DateTime>();
            if (configs == null)
                return result;

            var daysInMonth = DateTime.DaysInMonth(year, month);

            foreach (var cfg in configs)
            {
                if (cfg == null)
                    continue;

                if (cfg.PatternType == WeekOffPatternType.NthWeekdayOfMonth)
                {
                    var occurrences = (cfg.Occurrences ?? Enumerable.Empty<WeekOffOccurrence>())
                        .Select(o => o.OccurrenceNumber)
                        .Where(n => n >= 1 && n <= 5)
                        .Distinct();

                    foreach (var occurrence in occurrences)
                    {
                        var date = GetNthWeekdayOfMonth(year, month, cfg.Day, occurrence);
                        if (date.HasValue)
                            result.Add(date.Value.Date);
                    }
                }
                else
                {
                    // EveryWeek (default/legacy): every date in the month
                    // that falls on the configured weekday.
                    for (int day = 1; day <= daysInMonth; day++)
                    {
                        var date = new DateTime(year, month, day);
                        if (date.DayOfWeek == cfg.Day)
                            result.Add(date);
                    }
                }
            }

            return result;
        }

        // Union of week-off dates across every month touched by
        // [start, end] (inclusive), clipped to that range. Handles a date
        // range spanning more than one calendar month (e.g. the dashboard's
        // rolling 30-day trend), where the "Nth occurrence" numbering must
        // be recomputed per month.
        public static HashSet<DateTime> GetWeekOffDatesInRange(DateTime start, DateTime end, IEnumerable<WeekOff>? configs)
        {
            var result = new HashSet<DateTime>();
            if (configs == null || start > end)
                return result;

            var configList = configs as IList<WeekOff> ?? configs.ToList();

            var cursor = new DateTime(start.Year, start.Month, 1);
            var endMonth = new DateTime(end.Year, end.Month, 1);

            while (cursor <= endMonth)
            {
                foreach (var date in GetWeekOffDatesForMonth(cursor.Year, cursor.Month, configList))
                {
                    if (date >= start.Date && date <= end.Date)
                        result.Add(date);
                }

                cursor = cursor.AddMonths(1);
            }

            return result;
        }

        // Convenience: is this single date a week-off per the given
        // configs?
        public static bool IsWeekOffDate(DateTime date, IEnumerable<WeekOff>? configs)
        {
            return GetWeekOffDatesForMonth(date.Year, date.Month, configs).Contains(date.Date);
        }
    }
}
