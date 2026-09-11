using System;
using System.Text.RegularExpressions;

namespace Infrastructure.EsslIntegration
{
    /// <summary>
    /// Whitelist-validates eTimeTrackLite1 DeviceLogs table names before they
    /// are ever concatenated into dynamic SQL (see
    /// EsslAttendanceDataSource.DiscoverDeviceLogTablesAsync/GetDeviceLogsAsync).
    ///
    /// eTimeTrackLite1 stores raw punches either in a single legacy base
    /// table, or - on installations that partition by month - in
    /// {base}_{month}_{year} (e.g. DeviceLogs_8_2026 for August 2026), or
    /// both at once. The base table itself has been observed under TWO
    /// different real-world spellings across different installs: "DeviceLogs"
    /// (no separator) and "Device_Logs" (underscore between the two words) -
    /// this class was originally hardcoded to only the first, which meant an
    /// install using "Device_Logs" / "Device_Logs_1_2026" .. "Device_Logs_9_2026"
    /// silently discovered ZERO tables every run (the whitelist rejected every
    /// one of its real table names) - historical AND current sync both
    /// completed "successfully" having found nothing, with no error to
    /// explain why. Both spellings are accepted now, for both the base table
    /// and its monthly partitions, so this no longer assumes one specific
    /// installation's naming convention. Only names matching one of these
    /// shapes are ever considered "safe" - anything else discovered under
    /// dbo (a view, a differently-named table, a table an admin happened to
    /// name similarly) is silently ignored rather than queried.
    ///
    /// This class never talks to a database - it is a pure, unit-testable
    /// name validator. The actual "does this table exist right now" check is
    /// sys.tables/sys.schemas metadata, done separately in
    /// EsslAttendanceDataSource.
    /// </summary>
    public static class EsslDeviceLogTableName
    {
        /// <summary>
        /// Canonical/default spelling of the base (non-partitioned) table -
        /// used only as a fallback default value (see EsslDeviceLogRaw.
        /// SourceTable) and in log/comment text. NEVER used for validation
        /// any more - IsBaseTableName accepts "Device_Logs" too. Do not add
        /// new "== BaseTableName" comparisons; use IsBaseTableName instead.
        /// </summary>
        public const string BaseTableName = "DeviceLogs";

        /// <summary>
        /// Matches, case-sensitively (SQL Server default collation is
        /// case-insensitive anyway, but this keeps the C#-side check exact):
        ///   DeviceLogs                    (base, no separator)
        ///   Device_Logs                   (base, underscore separator)
        ///   DeviceLogs_&lt;1-12&gt;_&lt;yyyy&gt;    (monthly, no separator before the words)
        ///   Device_Logs_&lt;1-12&gt;_&lt;yyyy&gt;   (monthly, underscore separator)
        /// Deliberately does NOT accept a zero-padded month
        /// ("DeviceLogs_08_2026") since that has never been one of the
        /// confirmed real table names on any install seen so far; add that
        /// pattern here explicitly if a real deployment turns out to need it.
        /// </summary>
        private static readonly Regex TablePattern = new(
            @"^Device_?Logs(?:_(?<month>[1-9]|1[0-2])_(?<year>\d{4}))?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>True for any accepted spelling of the bare base table, or a well-formed monthly-partition name in either spelling - the ONLY names this integration will ever build dynamic SQL against.</summary>
        public static bool IsValidTableName(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            return TablePattern.IsMatch(tableName);
        }

        /// <summary>True for the bare base table specifically ("DeviceLogs" or "Device_Logs") - not a monthly partition.</summary>
        public static bool IsBaseTableName(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            var match = TablePattern.Match(tableName);
            return match.Success && !match.Groups["month"].Success;
        }

        /// <summary>
        /// True for the monthly-partitioned shape specifically (either
        /// spelling, never the bare base table). Extracts the calendar
        /// month/year the table name encodes, so the caller can decide
        /// whether that month overlaps the sync window without opening the
        /// table.
        /// </summary>
        public static bool TryParseMonthlyTable(string? tableName, out int year, out int month)
        {
            year = 0;
            month = 0;

            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            var match = TablePattern.Match(tableName);
            if (!match.Success || !match.Groups["month"].Success)
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
