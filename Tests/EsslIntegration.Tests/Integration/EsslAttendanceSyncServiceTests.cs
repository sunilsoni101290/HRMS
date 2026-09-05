using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Application.Services.Attendances;
using EsslIntegration.Tests.TestFixtures;
using FluentAssertions;
using Infrastructure.EsslIntegration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EsslIntegration.Tests.Integration
{
    /// <summary>
    /// EsslAttendanceSyncService.SyncAsync exercised end-to-end against a
    /// REAL ApplicationDbContext (EF Core InMemory) - only
    /// IEsslAttendanceDataSource (the actual eTimeTrackLite1 SQL connection),
    /// IAttendanceProcessorService, and IErrorLogService are mocked. Covers
    /// requirements #5/#6 (mapped + unmapped employee import) and #6/#8
    /// (idempotent repeated sync).
    /// </summary>
    public class EsslAttendanceSyncServiceTests
    {
        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EsslDatabase:Enabled"] = "true",
                    ["EsslDatabase:BatchSize"] = "500",
                    ["EsslDatabase:OverlapMinutes"] = "30"
                })
                .Build();

        private static EsslDeviceLogRaw Row(int deviceLogId, string userId, string direction, DateTime logDate, int deviceId = 1) =>
            new()
            {
                DeviceLogId = deviceLogId,
                DeviceId = deviceId,
                UserId = userId,
                LogDate = logDate,
                Direction = direction
            };

        // Real IDataProtectionProvider (ephemeral, in-memory keys) - the
        // service only uses this to Protect/Unprotect the saved database
        // password, so a real provider is simpler and safer here than mocking
        // IDataProtector's Protect(byte[])/Unprotect(byte[]) plumbing.
        private static IDataProtectionProvider BuildDataProtectionProvider() =>
            DataProtectionProvider.Create("EsslIntegration.Tests");

        private static (EsslAttendanceSyncService Service, Mock<IEsslAttendanceDataSource> DataSourceMock) BuildService(
            Infrastructure.ApplicationDbContext db,
            List<EsslDeviceLogRaw> rowsToReturn,
            List<string>? tablesToDiscover = null)
        {
            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            dataSourceMock
                .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            dataSourceMock
                .Setup(x => x.DiscoverDeviceLogTablesAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tablesToDiscover ?? new List<string> { "DeviceLogs" });
            dataSourceMock
                .Setup(x => x.GetDeviceLogsAsync(
                    It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                    It.IsAny<EsslDeviceLogCursor?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, IReadOnlyList<string> _, DateTime _, DateTime _, EsslDeviceLogCursor? cursor, int _, CancellationToken _) =>
                    // Mimic the real data source's keyset pagination well
                    // enough for the SyncAsync while-loop to terminate:
                    // once a cursor has been handed back (meaning the
                    // service has already consumed every row once), the
                    // second call in the same run returns nothing further.
                    cursor == null ? rowsToReturn : new List<EsslDeviceLogRaw>());

            var processorMock = new Mock<IAttendanceProcessorService>();
            processorMock.Setup(x => x.ProcessAttendanceAsync()).ReturnsAsync(true);

            var errorLogMock = new Mock<IErrorLogService>();
            errorLogMock
                .Setup(x => x.LogAsync(
                    It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var service = new EsslAttendanceSyncService(
                db,
                dataSourceMock.Object,
                processorMock.Object,
                errorLogMock.Object,
                BuildConfig(),
                BuildDataProtectionProvider(),
                NullLogger<EsslAttendanceSyncService>.Instance);

            return (service, dataSourceMock);
        }

        [Fact]
        public async Task Sync_ImportsMappedPunch_AndFlagsUnmappedEmployee_WithoutDiscardingIt()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var rows = new List<EsslDeviceLogRaw>
            {
                Row(101, EsslTestFixture.MappedBiometricCode, "IN", now.AddMinutes(-30)),
                Row(102, EsslTestFixture.UnmappedBiometricCode, "OUT", now.AddMinutes(-20))
            };

            var (service, _) = BuildService(fixture.Context, rows);

            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeTrue();
            result.RecordsFound.Should().Be(2);
            result.RecordsImported.Should().Be(2);
            result.UnknownEmployeeCount.Should().Be(1);
            result.ErrorCount.Should().Be(0);

            // Requirement #21 - the unmapped employee's punch must still be
            // preserved, never silently discarded.
            var savedRows = await fixture.Context.BiometricAttendanceLogs.ToListAsync();
            savedRows.Should().HaveCount(2);
            savedRows.Should().Contain(x => x.EmployeeCode == EsslTestFixture.UnmappedBiometricCode);
        }

        [Fact]
        public async Task Sync_IsIdempotent_SecondRunOverSameWindowImportsNothingNew()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var rows = new List<EsslDeviceLogRaw>
            {
                Row(201, EsslTestFixture.MappedBiometricCode, "IN", now.AddMinutes(-30)),
                Row(202, EsslTestFixture.MappedBiometricCode, "OUT", now.AddMinutes(-10))
            };

            var (service, _) = BuildService(fixture.Context, rows);

            var firstRun = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");
            firstRun.RecordsImported.Should().Be(2);

            // Same underlying eTimeTrackLite rows "seen" again (e.g. the
            // automatic incremental window's overlap re-scanned them, or a
            // manual historical re-run covers the same dates) - requirement
            // #6: "Run 2 -> 0 duplicate punches" imported again.
            var secondRun = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            secondRun.RecordsImported.Should().Be(0);
            secondRun.RecordsSkipped.Should().Be(2);
            secondRun.DuplicateCount.Should().Be(2);

            var savedRows = await fixture.Context.BiometricAttendanceLogs.ToListAsync();
            savedRows.Should().HaveCount(2, "the second run must not create duplicate rows");
        }

        [Fact]
        public async Task Sync_WhenDisabled_ReturnsWithoutTouchingDatabase()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            dataSourceMock
                .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var processorMock = new Mock<IAttendanceProcessorService>();
            var errorLogMock = new Mock<IErrorLogService>();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["EsslDatabase:Enabled"] = "false" })
                .Build();

            var service = new EsslAttendanceSyncService(
                fixture.Context, dataSourceMock.Object, processorMock.Object, errorLogMock.Object,
                config, BuildDataProtectionProvider(), NullLogger<EsslAttendanceSyncService>.Instance);

            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeFalse();
            (await fixture.Context.BiometricAttendanceLogs.CountAsync()).Should().Be(0);
        }

        // ==================================================================
        // MONTHLY DEVICELOGS TABLE DISCOVERY / TABLE-QUALIFIED IDEMPOTENCY
        // ==================================================================

        [Fact]
        public async Task Sync_WhenNoDeviceLogTablesDiscovered_SucceedsWithZeroRecords()
        {
            // "Missing monthly table" scenario - e.g. a historical import for
            // a month that was never partitioned, or the database is
            // temporarily unreachable for metadata queries. Must not be
            // treated as a hard failure.
            using var fixture = await EsslTestFixture.CreateAsync();

            var (service, _) = BuildService(fixture.Context, new List<EsslDeviceLogRaw>(), tablesToDiscover: new List<string>());

            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeTrue();
            result.RecordsFound.Should().Be(0);
            result.TablesScanned.Should().BeEmpty();
            (await fixture.Context.BiometricAttendanceLogs.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Sync_ReportsDiscoveredTables_OnTheResult()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var rows = new List<EsslDeviceLogRaw>
            {
                new()
                {
                    DeviceLogId = 91,
                    DeviceId = 19,
                    UserId = EsslTestFixture.MappedBiometricCode,
                    LogDate = now.AddMinutes(-30),
                    Direction = "in",
                    SourceTable = "DeviceLogs_8_2026"
                }
            };

            var (service, _) = BuildService(fixture.Context, rows, tablesToDiscover: new List<string> { "DeviceLogs", "DeviceLogs_8_2026" });

            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeTrue();
            result.TablesScanned.Should().BeEquivalentTo(new[] { "DeviceLogs", "DeviceLogs_8_2026" });
        }

        [Fact]
        public void BuildDeviceTransactionId_ForBaseDeviceLogsTable_UsesLegacyUnqualifiedFormat()
        {
            // Backward compatibility: rows from the bare "DeviceLogs" table
            // must keep matching whatever format production already saved
            // before monthly-table support existed.
            var raw = new EsslDeviceLogRaw { DeviceLogId = 91, SourceTable = "DeviceLogs" };

            EsslAttendanceSyncService.BuildDeviceTransactionId(raw).Should().Be("ESSL-91");
        }

        [Fact]
        public void BuildDeviceTransactionId_ForMonthlyTable_IsTableQualified()
        {
            var raw = new EsslDeviceLogRaw { DeviceLogId = 91, SourceTable = "DeviceLogs_8_2026" };

            EsslAttendanceSyncService.BuildDeviceTransactionId(raw).Should().Be("ESSL-DeviceLogs_8_2026-91");
        }

        [Fact]
        public void BuildDeviceTransactionId_SameDeviceLogId_DifferentMonthlyTables_ProducesDistinctIds()
        {
            // The exact scenario that makes the base-table-only format
            // unsafe once monthly partitions exist: DeviceLogId is only an
            // IDENTITY within one physical table, so the SAME id can appear
            // in two different monthly tables for two genuinely different
            // punches. The idempotency key must disambiguate them.
            var rawAugust = new EsslDeviceLogRaw { DeviceLogId = 91, SourceTable = "DeviceLogs_8_2026" };
            var rawSeptember = new EsslDeviceLogRaw { DeviceLogId = 91, SourceTable = "DeviceLogs_9_2026" };

            var idAugust = EsslAttendanceSyncService.BuildDeviceTransactionId(rawAugust);
            var idSeptember = EsslAttendanceSyncService.BuildDeviceTransactionId(rawSeptember);

            idAugust.Should().NotBe(idSeptember);
        }

        [Fact]
        public async Task Sync_UsesLogDate_NotDownloadDate_ForAttendanceProcessing()
        {
            // The exact scenario called out by the integration requirement:
            // DeviceLogId 91 / DeviceId 19 / UserId 2, LogDate 2026-08-04,
            // DownloadDate 2026-08-31 (downloaded 27 days after the actual
            // punch). The imported row's PunchTime must be the August 4th
            // LogDate, never the August 31st DownloadDate.
            using var fixture = await EsslTestFixture.CreateAsync();

            var logDate = new DateTime(2026, 8, 4, 16, 14, 9);
            var downloadDate = new DateTime(2026, 8, 31, 18, 30, 9);

            var rows = new List<EsslDeviceLogRaw>
            {
                new()
                {
                    DeviceLogId = 91,
                    DeviceId = 19,
                    UserId = EsslTestFixture.MappedBiometricCode,
                    LogDate = logDate,
                    DownloadDate = downloadDate,
                    Direction = "in",
                    SourceTable = "DeviceLogs"
                }
            };

            var (service, _) = BuildService(fixture.Context, rows);

            var result = await service.SyncAsync(
                new EsslSyncRequestDto { FromDate = new DateTime(2026, 8, 1), ToDate = new DateTime(2026, 8, 31) },
                EsslTestFixture.TenantId,
                "Test");

            result.RecordsImported.Should().Be(1);

            var saved = (await fixture.Context.BiometricAttendanceLogs.ToListAsync()).Single();
            saved.PunchTime.Should().Be(logDate);
            saved.DownloadDate.Should().Be(downloadDate);
            saved.SourceTable.Should().Be("DeviceLogs");
        }
    }
}
