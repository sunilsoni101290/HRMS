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
    /// Phase 1 audit - regression coverage for the concurrency/locking
    /// mechanism actually implemented today: EsslAttendanceSyncState.
    /// IsSyncRunning, guarded by EsslAttendanceSyncService.SyncAsync's own
    /// internal claim (lockAlreadyClaimed=false, the automatic background
    /// cycle's path) and by the separate ClaimSyncLockAsync entry point
    /// (used by the manual "Sync Now" controller action BEFORE the job is
    /// even enqueued - see IEsslAttendanceSyncService's remarks). A lock
    /// held longer than the service's internal stale-lock threshold (30
    /// minutes - DefaultStaleLockMinutes, not exposed publicly, so these
    /// tests drive it via LastSyncStartedAt directly on the persisted
    /// state row) is treated as abandoned and taken over rather than
    /// blocking the feature forever.
    /// </summary>
    public class EsslAttendanceSyncServiceLockingTests
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

        private static IDataProtectionProvider BuildDataProtectionProvider() =>
            DataProtectionProvider.Create("EsslIntegration.Tests");

        private static EsslAttendanceSyncService BuildService(
            Infrastructure.ApplicationDbContext db,
            List<EsslDeviceLogRaw>? rowsToReturn = null)
        {
            var dataSourceMock = new Mock<IEsslAttendanceDataSource>();
            dataSourceMock
                .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            dataSourceMock
                .Setup(x => x.DiscoverDeviceLogTablesAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "DeviceLogs" });
            dataSourceMock
                .Setup(x => x.GetDeviceLogsAsync(
                    It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                    It.IsAny<EsslDeviceLogCursor?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, IReadOnlyList<string> _, DateTime _, DateTime _, EsslDeviceLogCursor? cursor, int _, CancellationToken _) =>
                    cursor == null ? (rowsToReturn ?? new List<EsslDeviceLogRaw>()) : new List<EsslDeviceLogRaw>());

            var processorMock = new Mock<IAttendanceProcessorService>();
            processorMock
                .Setup(x => x.ProcessAttendanceWithResultAsync(It.IsAny<int>()))
                .ReturnsAsync(new AttendanceProcessingResultDto { Success = true });

            var errorLogMock = new Mock<IErrorLogService>();
            errorLogMock
                .Setup(x => x.LogAsync(
                    It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var bulkWriterMock = new Mock<IEsslBulkAttendanceLogWriter>();
            bulkWriterMock
                .Setup(x => x.StageAndMergeAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<EsslBulkStageRow>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Bulk staging SQL objects not available in this test environment (EF InMemory)."));

            return new EsslAttendanceSyncService(
                db,
                dataSourceMock.Object,
                processorMock.Object,
                errorLogMock.Object,
                BuildConfig(),
                BuildDataProtectionProvider(),
                bulkWriterMock.Object,
                NullLogger<EsslAttendanceSyncService>.Instance);
        }

        [Fact]
        public async Task ClaimSyncLockAsync_FirstCall_SucceedsAndPersistsIsSyncRunning()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var service = BuildService(fixture.Context);

            var (success, _) = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);

            success.Should().BeTrue();

            var state = await fixture.Context.EsslAttendanceSyncStates
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);
            state.IsSyncRunning.Should().BeTrue();
            state.LastSyncStartedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task ClaimSyncLockAsync_WhileAlreadyRunning_IsRefused()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var service = BuildService(fixture.Context);

            var first = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            first.Success.Should().BeTrue();

            // Second, overlapping claim - e.g. the manual "Sync Now" button
            // clicked twice, or the automatic cycle firing while a manual
            // historical import is still in progress. Must be refused, not
            // silently allowed to proceed.
            var second = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);

            second.Success.Should().BeFalse();
            second.Message.Should().Contain("already in progress");
        }

        [Fact]
        public async Task SyncAsync_WhileAnotherSyncIsRunning_RefusesAndDoesNotTouchTheDatabase()
        {
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var service = BuildService(fixture.Context, new List<EsslDeviceLogRaw>
            {
                new()
                {
                    DeviceLogId = 1,
                    DeviceId = 1,
                    UserId = EsslTestFixture.MappedBiometricCode,
                    LogDate = now.AddMinutes(-5),
                    Direction = "IN"
                }
            });

            // Simulate a genuinely in-progress run (e.g. the background
            // service's automatic cycle) by claiming the lock directly,
            // exactly as SyncAsync's own internal claim would.
            var claim = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            claim.Success.Should().BeTrue();

            // A second, concurrent/overlapping SyncAsync call for the SAME
            // tenant (lockAlreadyClaimed defaults to false - this is the
            // automatic background cycle's own code path) must refuse to
            // run rather than racing the first one.
            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("already in progress");
            (await fixture.Context.BiometricAttendanceLogs.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task SyncAsync_WithLockAlreadyClaimed_SkipsItsOwnClaim_AndRunsNormally()
        {
            // This is the manual "Sync Now" path's actual contract: the
            // controller calls ClaimSyncLockAsync itself first (so the
            // lock/state row are committed before it responds to the
            // browser), then hands the job to the SAME background consumer
            // that also runs SyncAsync - passing lockAlreadyClaimed=true so
            // SyncAsync does not see its own just-claimed lock and
            // incorrectly refuse to run (see IEsslAttendanceSyncService's
            // remarks).
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var service = BuildService(fixture.Context, new List<EsslDeviceLogRaw>
            {
                new()
                {
                    DeviceLogId = 1,
                    DeviceId = 1,
                    UserId = EsslTestFixture.MappedBiometricCode,
                    LogDate = now.AddMinutes(-5),
                    Direction = "IN"
                }
            });

            var claim = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            claim.Success.Should().BeTrue();

            var result = await service.SyncAsync(
                new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test", lockAlreadyClaimed: true);

            result.Success.Should().BeTrue("a caller that already holds the lock itself must not be blocked by its own claim");
            result.RecordsImported.Should().Be(1);
        }

        [Fact]
        public async Task SyncAsync_AfterAStaleLock_TakesOverInsteadOfBlockingForever()
        {
            // A lock left "Running" by a process that crashed/was killed
            // mid-run (LastSyncStartedAt well past the service's internal
            // stale-lock threshold) must be taken over automatically, not
            // treated as a permanently blocked tenant.
            using var fixture = await EsslTestFixture.CreateAsync();

            var now = DateTime.Now;
            var service = BuildService(fixture.Context, new List<EsslDeviceLogRaw>
            {
                new()
                {
                    DeviceLogId = 1,
                    DeviceId = 1,
                    UserId = EsslTestFixture.MappedBiometricCode,
                    LogDate = now.AddMinutes(-5),
                    Direction = "IN"
                }
            });

            var claim = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            claim.Success.Should().BeTrue();

            // Back-date the claim well past the 30-minute stale-lock
            // threshold, simulating an abandoned lock.
            var state = await fixture.Context.EsslAttendanceSyncStates
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);
            state.LastSyncStartedAt = DateTime.Now.AddMinutes(-45);
            await fixture.Context.SaveChangesAsync();

            var result = await service.SyncAsync(new EsslSyncRequestDto(), EsslTestFixture.TenantId, "Test");

            result.Success.Should().BeTrue("a lock held past the stale threshold must be taken over, not block forever");
            result.RecordsImported.Should().Be(1);
        }

        [Fact]
        public async Task ResetStuckSyncAsync_WhileGenuinelyRunning_RefusesToReset()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            var claim = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            claim.Success.Should().BeTrue();

            var (success, message) = await service.ResetStuckSyncAsync(EsslTestFixture.TenantId);

            success.Should().BeFalse("a lock younger than the stale threshold may still be a genuinely active sync");
            message.Should().NotBeNullOrEmpty();

            var state = await fixture.Context.EsslAttendanceSyncStates
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);
            state.IsSyncRunning.Should().BeTrue("a refused reset must not clear the lock");
        }

        [Fact]
        public async Task ResetStuckSyncAsync_ForAStaleLock_ClearsIt()
        {
            using var fixture = await EsslTestFixture.CreateAsync();
            var service = BuildService(fixture.Context);

            var claim = await service.ClaimSyncLockAsync(EsslTestFixture.TenantId);
            claim.Success.Should().BeTrue();

            var state = await fixture.Context.EsslAttendanceSyncStates
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);
            state.LastSyncStartedAt = DateTime.Now.AddMinutes(-45);
            await fixture.Context.SaveChangesAsync();

            var (success, _) = await service.ResetStuckSyncAsync(EsslTestFixture.TenantId);

            success.Should().BeTrue();

            var reloaded = await fixture.Context.EsslAttendanceSyncStates
                .FirstAsync(x => x.TenantId == EsslTestFixture.TenantId);
            reloaded.IsSyncRunning.Should().BeFalse();
            reloaded.LastSyncStatus.Should().Be("Cancelled");
        }
    }
}
