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
            List<EsslDeviceLogRaw> rowsToReturn)
        {
            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            dataSourceMock
                .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            dataSourceMock
                .Setup(x => x.GetDeviceLogsAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rowsToReturn);

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
    }
}
