using Application.DTOs.Attendances;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Attendances
{
    /// <summary>
    /// Standalone "Sync Biometric Attendance (Month Wise)" admin tool - a
    /// brand-new, additive feature, completely separate from
    /// IEsslAttendanceSyncService and IHistoricalAttendanceSyncService (never
    /// referenced here, never modified). Wraps a single call to
    /// dbo.usp_SyncBiometricAttendance_MonthWise (see the root-level
    /// "add MonthWise Biometric Attendance Sync stored procedure.sql"
    /// script), which takes the FIRST punch of each mapped employee per
    /// calendar day for the given month and inserts one Attendance row per
    /// employee/day using the single ShiftId/Status the admin picked - it
    /// does not resolve a per-employee shift or run any of the
    /// AttendanceService business rules.
    /// </summary>
    public interface IMonthWiseBiometricSyncService
    {
        /// <summary>
        /// Runs the SP synchronously and returns its single result-set row.
        /// This is a bounded, single set-based/cursor-driven run over one
        /// tenant/company/shift/month (never an open-ended historical
        /// range), so it runs inline on the request rather than being
        /// queued onto a background job like the eSSL/Historical engines.
        /// </summary>
        Task<MonthWiseSyncResultDto> SyncAsync(MonthWiseSyncRequestDto request, string tenantId, CancellationToken ct = default);
    }
}
