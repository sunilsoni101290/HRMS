using Application.Interfaces.ErrorLog;
using Application.Services.Attendances;
using AttendanceProcessing.Tests.TestFixtures;
using Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Domain.Enums.EnumExtensions;

namespace AttendanceProcessing.Tests.Integration
{
    /// <summary>
    /// Phase 1 audit - regression coverage for AttendanceService's
    /// IsLate/IsEarlyExit computation (PunchInCoreAsync "STEP 9: LATE
    /// CHECK" / PunchOutCoreAsync "STEP 10: EARLY EXIT CHECK"), replayed
    /// through the same biometric pipeline (AttendanceProcessorService ->
    /// AttendanceService) as production, against the day-shift fixture
    /// (General Shift: 09:00-18:00, GraceInMinutes=10, GraceOutMinutes=10).
    /// allowedIn = shiftStart + GraceInMinutes = 09:10; allowedOut =
    /// shiftEnd - GraceOutMinutes = 17:50 - both strict comparisons
    /// (IsLate = now &gt; allowedIn, IsEarlyExit = now &lt; allowedOut), so
    /// a punch exactly ON the boundary is NOT late/early.
    /// </summary>
    public class AttendanceLateEarlyExitTests
    {
        private static (AttendanceProcessorService processor, Mock<IErrorLogService> errorLog) BuildProcessor(AttendanceProcessingTestFixture fixture)
        {
            var errorLogMock = new Mock<IErrorLogService>();
            errorLogMock
                .Setup(x => x.LogAsync(
                    It.IsAny<Exception>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                    It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var attendanceService = new AttendanceService(fixture.Context);

            var processor = new AttendanceProcessorService(
                fixture.Context,
                attendanceService,
                errorLogMock.Object,
                NullLogger<AttendanceProcessorService>.Instance);

            return (processor, errorLogMock);
        }

        [Fact]
        public async Task PunchIn_AfterGraceWindow_IsFlaggedLate()
        {
            using var fixture = await AttendanceProcessingTestFixture.CreateAsync();
            var (processor, _) = BuildProcessor(fixture);

            // Shift starts 09:00, GraceInMinutes=10 -> allowedIn = 09:10.
            // 09:15 is after that.
            var punchTime = new DateTime(2026, 8, 24, 9, 15, 0);
            fixture.Context.BiometricAttendanceLogs.Add(
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, punchTime, PunchType.In));
            await fixture.Context.SaveChangesAsync();

            await processor.ProcessAttendanceAsync();

            var attendance = await fixture.Context.Attendances
                .FirstAsync(x => x.EmployeeId == AttendanceProcessingTestFixture.EmployeeId);

            attendance.IsLate.Should().BeTrue();
        }

        [Fact]
        public async Task PunchIn_WithinGraceWindow_IsNotFlaggedLate()
        {
            using var fixture = await AttendanceProcessingTestFixture.CreateAsync();
            var (processor, _) = BuildProcessor(fixture);

            // 09:05 is before allowedIn (09:10) - within the grace period.
            var punchTime = new DateTime(2026, 8, 24, 9, 5, 0);
            fixture.Context.BiometricAttendanceLogs.Add(
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, punchTime, PunchType.In));
            await fixture.Context.SaveChangesAsync();

            await processor.ProcessAttendanceAsync();

            var attendance = await fixture.Context.Attendances
                .FirstAsync(x => x.EmployeeId == AttendanceProcessingTestFixture.EmployeeId);

            attendance.IsLate.Should().BeFalse();
        }

        [Fact]
        public async Task PunchOut_BeforeGraceWindow_IsFlaggedEarlyExit()
        {
            using var fixture = await AttendanceProcessingTestFixture.CreateAsync();
            var (processor, _) = BuildProcessor(fixture);

            var inTime = new DateTime(2026, 8, 24, 9, 0, 0);
            // Shift ends 18:00, GraceOutMinutes=10 -> allowedOut = 17:50.
            // 17:30 is before that.
            var outTime = new DateTime(2026, 8, 24, 17, 30, 0);

            fixture.Context.BiometricAttendanceLogs.AddRange(
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, inTime, PunchType.In),
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, outTime, PunchType.Out));
            await fixture.Context.SaveChangesAsync();

            await processor.ProcessAttendanceAsync();

            var attendance = await fixture.Context.Attendances
                .FirstAsync(x => x.EmployeeId == AttendanceProcessingTestFixture.EmployeeId);

            attendance.IsEarlyExit.Should().BeTrue();
        }

        [Fact]
        public async Task PunchOut_AtOrAfterGraceWindow_IsNotFlaggedEarlyExit()
        {
            using var fixture = await AttendanceProcessingTestFixture.CreateAsync();
            var (processor, _) = BuildProcessor(fixture);

            var inTime = new DateTime(2026, 8, 24, 9, 0, 0);
            // 17:55 is after allowedOut (17:50).
            var outTime = new DateTime(2026, 8, 24, 17, 55, 0);

            fixture.Context.BiometricAttendanceLogs.AddRange(
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, inTime, PunchType.In),
                fixture.MakePunch(AttendanceProcessingTestFixture.BiometricDeviceCode, outTime, PunchType.Out));
            await fixture.Context.SaveChangesAsync();

            await processor.ProcessAttendanceAsync();

            var attendance = await fixture.Context.Attendances
                .FirstAsync(x => x.EmployeeId == AttendanceProcessingTestFixture.EmployeeId);

            attendance.IsEarlyExit.Should().BeFalse();
        }

        [Fact]
        public async Task NightShift_LateCheck_AccountsForTheShiftStartingTheDayBefore()
        {
            // Night shift 22:00 -> 06:00, GraceInMinutes=10 -> allowedIn =
            // 22:10 on the shift's OWN start date. A punch at 22:20 is late.
            using var fixture = await AttendanceProcessingTestFixture.CreateAsync();
            var (processor, _) = BuildProcessor(fixture);

            var punchTime = new DateTime(2026, 8, 24, 22, 20, 0);
            fixture.Context.BiometricAttendanceLogs.Add(
                fixture.MakePunch(AttendanceProcessingTestFixture.NightShiftBiometricDeviceCode, punchTime, PunchType.In));
            await fixture.Context.SaveChangesAsync();

            await processor.ProcessAttendanceAsync();

            var attendance = await fixture.Context.Attendances
                .FirstAsync(x => x.EmployeeId == AttendanceProcessingTestFixture.NightShiftEmployeeId);

            attendance.IsLate.Should().BeTrue();
            attendance.Date.Date.Should().Be(new DateTime(2026, 8, 24));
        }
    }
}
