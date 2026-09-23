using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class WeekOff : BaseEntity
    {
        // The configured weekday.
        //  - PatternType == EveryWeek: this weekday is a week-off every
        //    single week (original/legacy meaning - unchanged behavior for
        //    every row that existed before this feature).
        //  - PatternType == NthWeekdayOfMonth: this weekday is the one whose
        //    Nth occurrence(s) in the month (see Occurrences) are a
        //    week-off, e.g. Day = Saturday with Occurrences {2, 4} for
        //    "2nd and 4th Saturday".
        public DayOfWeek Day { get; set; }

        // Additive: which pattern this row represents. Defaults to
        // EveryWeek so every existing row (created before this column
        // existed) keeps behaving exactly as before - see the root-level
        // "add WeekOff pattern type and occurrences.sql" script, which
        // backfills this column to EveryWeek (0) for all pre-existing rows.
        public WeekOffPatternType PatternType { get; set; } = WeekOffPatternType.EveryWeek;

        // Only populated when PatternType == NthWeekdayOfMonth. Modeled as
        // a child table (one row per selected occurrence number, 1-5),
        // matching this project's existing convention for a Masters entity
        // with a bounded multi-select child config (HolidayGroup /
        // HolidayGroupDetail) rather than a delimited string column.
        public ICollection<WeekOffOccurrence>? Occurrences { get; set; }
    }

    // Child row of WeekOff: one selected "Nth occurrence of the month" value
    // (1 = first, 2 = second, ... 5 = fifth) for a WeekOff configured with
    // PatternType == NthWeekdayOfMonth. E.g. "2nd and 4th Saturday" is one
    // WeekOff row (Day = Saturday, PatternType = NthWeekdayOfMonth) plus two
    // WeekOffOccurrence child rows (OccurrenceNumber = 2 and 4).
    public class WeekOffOccurrence : BaseEntity
    {
        [ForeignKey(nameof(WeekOff))]
        public string WeekOffId { get; set; }
        public WeekOff? WeekOff { get; set; }

        // 1-5 (there is never a 6th occurrence of a weekday in a month).
        public int OccurrenceNumber { get; set; }
    }
}
