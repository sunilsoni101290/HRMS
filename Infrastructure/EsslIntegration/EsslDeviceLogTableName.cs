using System;
using System.Text.RegularExpressions;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Whitelist-validates eTimeTrackLite1 DeviceLogs table names before they
    /// are ever concatenated into dynamic SQL (see
    /// EsslAttendanceDataSource.DiscoverDeviceLogTablesAsync/GetDeviceLogsAsync).
    ///
    /// eTimeTrackLite1 stores raw punches either in the single legacy
    /// [dbo].[DeviceLogs] table, or - on installations that partition by
    /// month - in [dbo].[DeviceLogs_{month}_{year}] (e.g. DeviceLogs_8_2026
    /// for August 2026), or both at once. Only names matching EXACTLY one of
    /// these two shapes are ever considered "safe" - anything else discovered
    /// under dbo (a view, a differently-named table, a table an admin
    /// happened to name similarly) is silently ignored rather than queried.
    ///
    /// This class never talks to a database - it is a pure, unit-testable
    /// name validator. The actual "does this table exist right now" check is
    /// sys.tables/sys.schemas metadata, done separately in
    /// EsslAttendanceDataSource.
    /// </summary>
    public static class EsslDeviceLogTableName
    {
        public const string BaseTableName = "DeviceLogs";

        // DeviceLogs_<month 1-12, no leading zero>_<4-digit year>. Matches
        // the exact examples in the integration requirement
        // (DeviceLogs_1_2026 .. DeviceLogs_12_2026) - deliberately does NOT
        // accept a zero-padded month ("DeviceLogs_08_2026") since that was
        // never one of the confirmed real table names; add that pattern here
        // explicitly if a real deployment turns out to use it.
        private static readonly Regex MonthlyPattern =
            new(@"^DeviceLogs_(?<month>[1-9]|1[0-2])_(?<year>\d{4})$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>True for "DeviceLogs" itself or a well-formed "DeviceLogs_M_YYYY" name - the ONLY names this integration will ever build dynamic SQL against.</summary>
        public static bool IsValidTableName(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            return tableName == BaseTableName || MonthlyPattern.IsMatch(tableName);
        }

        /// <summary>
        /// True for the monthly-partitioned shape specifically (not the bare
        /// "DeviceLogs" table). Extracts the calendar month/year the table
        /// name encodes, so the caller can decide whether that month
        /// overlaps the sync window without opening the table.
        /// </summary>
        public static bool TryParseMonthlyTable(string? tableName, out int year, out int month)
        {
            year = 0;
            month = 0;

            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            var match = MonthlyPattern.Match(tableName);
            if (!match.Success)
                return false;

            month = int.Parse(match.Groups["month"].Value);
            year = int.Parse(match.Groups["year"].Value);
            return true;
        }

        /// <summary>
        /// True when the monthly table's calendar month has any overlap with
        /// [fromDateInclusive, toDateExclusive) - used to skip discovered
        /// monthly tables that are outside the requested sync window instead
        /// of scanning every monthly table that has ever existed.
        /// </summary>
        public static bool MonthOverlapsRange(int year, int month, DateTime fromDateInclusive, DateTime toDateExclusive)
        {
            var monthStart = new DateTime(year, month, 1);
            var monthEndExclusive = monthStart.AddMonths(1);

            return monthStart < toDateExclusive && monthEndExclusive > fromDateInclusive;
        }

        /// <summary>Safe to interpolate into dynamic SQL only after IsValidTableName has already returned true for the same value - brackets it as a schema-qualified identifier.</summary>
        public static string ToBracketedIdentifier(string tableName) => $"[dbo].[{tableName}]";
    }
}
