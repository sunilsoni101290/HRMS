using Application.Services.Attendances;
using FluentAssertions;
using Infrastructure.EsslIntegration;
using Xunit;
using static Domain.Enums.EnumExtensions;

namespace EsslIntegration.Tests.Unit
{
    /// <summary>
    /// Requirement #16 - eTimeTrackLite1's Direction/AttDirection is
    /// untrusted free text; this must not assume a fixed vocabulary and
    /// must fall back to alternating pairing when neither field is
    /// recognizable (requirement #16/#17's "configurable punch-pairing
    /// logic"). Exercises EsslAttendanceSyncService.ResolvePunchType
    /// directly (internal static, exposed via InternalsVisibleTo).
    /// </summary>
    public class PunchDirectionResolutionTests
    {
        private static EsslDeviceLogRaw Raw(string userId, string? direction = null, string? attDirection = null) =>
            new()
            {
                DeviceLogId = 1,
                DeviceId = 1,
                UserId = userId,
                LogDate = new DateTime(2026, 8, 28, 9, 0, 0),
                Direction = direction,
                AttDirection = attDirection
            };

        [Theory]
        [InlineData("IN")]
        [InlineData("in")]
        [InlineData("I")]
        [InlineData("Check-In")]
        [InlineData("CHECKIN")]
        public void RecognizedInTokens_ResolveToIn(string token)
        {
            var cache = new Dictionary<(string, DateTime), PunchType>();

            var result = EsslAttendanceSyncService.ResolvePunchType(Raw("E1", direction: token), cache, DateTime.Today);

            result.Should().Be(PunchType.In);
        }

        [Theory]
        [InlineData("OUT")]
        [InlineData("out")]
        [InlineData("O")]
        [InlineData("Check-Out")]
        [InlineData("CHECKOUT")]
        public void RecognizedOutTokens_ResolveToOut(string token)
        {
            var cache = new Dictionary<(string, DateTime), PunchType>();

            var result = EsslAttendanceSyncService.ResolvePunchType(Raw("E1", direction: token), cache, DateTime.Today);

            result.Should().Be(PunchType.Out);
        }

        [Fact]
        public void FallsBackToAttDirection_WhenDirectionIsNull()
        {
            var cache = new Dictionary<(string, DateTime), PunchType>();

            var result = EsslAttendanceSyncService.ResolvePunchType(
                Raw("E1", direction: null, attDirection: "OUT"), cache, DateTime.Today);

            result.Should().Be(PunchType.Out);
        }

        [Fact]
        public void UnrecognizedToken_FirstPunchOfDay_DefaultsToIn()
        {
            var cache = new Dictionary<(string, DateTime), PunchType>();

            var result = EsslAttendanceSyncService.ResolvePunchType(
                Raw("E1", direction: "???"), cache, DateTime.Today);

            result.Should().Be(PunchType.In);
        }

        [Fact]
        public void UnrecognizedToken_AlternatesWithPreviousPunchForSameEmployeeAndDay()
        {
            var day = DateTime.Today;
            var cache = new Dictionary<(string, DateTime), PunchType>
            {
                [("E1", day)] = PunchType.In
            };

            var result = EsslAttendanceSyncService.ResolvePunchType(Raw("E1", direction: "???"), cache, day);

            result.Should().Be(PunchType.Out);
        }

        [Fact]
        public void UnrecognizedToken_DoesNotAlternateAcrossDifferentEmployees()
        {
            var day = DateTime.Today;
            var cache = new Dictionary<(string, DateTime), PunchType>
            {
                [("E1", day)] = PunchType.In
            };

            // Different employee ("E2") on the same day - has no prior
            // entry of its own, so must default to In, not inherit E1's Out.
            var result = EsslAttendanceSyncService.ResolvePunchType(Raw("E2", direction: "???"), cache, day);

            result.Should().Be(PunchType.In);
        }
    }
}
