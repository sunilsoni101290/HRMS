using System;
using FluentAssertions;
using Infrastructure.EsslIntegration;
using Xunit;

namespace EsslIntegration.Tests.Unit
{
    /// <summary>
    /// Requirement #3 ("Monthly DeviceLog table discovery... never
    /// concatenate arbitrary user input directly into SQL. Validate the
    /// table name before using dynamic SQL") - pure, no-database tests for
    /// the whitelist that gates every table name before it can reach
    /// EsslAttendanceDataSource's dynamic SQL.
    /// </summary>
    public class EsslDeviceLogTableNameTests
    {
        [Theory]
        [InlineData("DeviceLogs")]
        [InlineData("DeviceLogs_1_2026")]
        [InlineData("DeviceLogs_8_2026")]
        [InlineData("DeviceLogs_12_2026")]
        [InlineData("DeviceLogs_9_2099")]
        public void IsValidTableName_AcceptsExpectedShapes(string name)
        {
            EsslDeviceLogTableName.IsValidTableName(name).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("DeviceLogs_0_2026")]        // month 0 is invalid
        [InlineData("DeviceLogs_13_2026")]        // month 13 is invalid
        [InlineData("DeviceLogs_08_2026")]        // zero-padded month not one of the confirmed real names
        [InlineData("DeviceLogs_8_26")]           // 2-digit year not accepted
        [InlineData("DeviceLogsBackup")]
        [InlineData("DeviceLogs_8_2026; DROP TABLE Users;--")]
        [InlineData("Employees")]
        [InlineData("DeviceLogs_8_2026_2")]
        public void IsValidTableName_RejectsEverythingElse(string? name)
        {
            EsslDeviceLogTableName.IsValidTableName(name).Should().BeFalse();
        }

        [Fact]
        public void TryParseMonthlyTable_ExtractsYearAndMonth()
        {
            EsslDeviceLogTableName.TryParseMonthlyTable("DeviceLogs_8_2026", out var year, out var month).Should().BeTrue();
            year.Should().Be(2026);
            month.Should().Be(8);
        }

        [Fact]
        public void TryParseMonthlyTable_ReturnsFalse_ForTheBaseTable()
        {
            EsslDeviceLogTableName.TryParseMonthlyTable("DeviceLogs", out _, out _).Should().BeFalse();
        }

        [Fact]
        public void MonthOverlapsRange_TrueWhenWindowFallsInsideTheMonth()
        {
            EsslDeviceLogTableName.MonthOverlapsRange(2026, 8, new DateTime(2026, 8, 1), new DateTime(2026, 8, 31))
                .Should().BeTrue();
        }

        [Fact]
        public void MonthOverlapsRange_TrueWhenWindowSpansMultipleMonths()
        {
            // A historical import request of "2026-07-15 to 2026-09-15"
            // must pick up July, August, AND September's monthly tables.
            EsslDeviceLogTableName.MonthOverlapsRange(2026, 8, new DateTime(2026, 7, 15), new DateTime(2026, 9, 15))
                .Should().BeTrue();
        }

        [Fact]
        public void MonthOverlapsRange_FalseWhenWindowIsInAnEarlierMonth()
        {
            EsslDeviceLogTableName.MonthOverlapsRange(2026, 9, new DateTime(2026, 8, 1), new DateTime(2026, 8, 31))
                .Should().BeFalse();
        }

        [Fact]
        public void MonthOverlapsRange_FalseWhenWindowIsInALaterMonth()
        {
            EsslDeviceLogTableName.MonthOverlapsRange(2026, 7, new DateTime(2026, 8, 1), new DateTime(2026, 8, 31))
                .Should().BeFalse();
        }

        [Fact]
        public void ToBracketedIdentifier_ProducesSchemaQualifiedBracketedName()
        {
            EsslDeviceLogTableName.ToBracketedIdentifier("DeviceLogs_8_2026").Should().Be("[dbo].[DeviceLogs_8_2026]");
        }
    }
}
