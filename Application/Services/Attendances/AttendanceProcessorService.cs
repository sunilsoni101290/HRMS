using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Turns raw BiometricAttendanceLog rows into real Attendance records.
    ///
    /// This used to hand-roll its own first-in/last-out logic (no shift
    /// awareness, no grace period, no late/early/overtime, no night-shift
    /// grouping, always Status = Present). That duplicated - badly - what
    /// AttendanceService already does correctly for the manual/web punch
    /// clock (PunchInAsync/PunchOutAsync/BreakInAsync/BreakOutAsync already
    /// resolve the employee's shift via EmployeeShiftMapping -> DefaultShift,
    /// handle night shifts that cross midnight, apply GraceIn/GraceOutMinutes,
    /// compute TotalWorkingHours net of breaks, OvertimeHours, IsLate,
    /// IsEarlyExit and the Present/HalfDay/Absent status). So each raw punch
    /// is now replayed through those exact same methods instead - the
    /// biometric path and the manual-punch path share one engine, per the
    /// "reuse the existing ERP attendance logic" requirement. This class's
    /// only remaining job is: resolve which raw punches map to which
    /// employee, replay them in order, and tag the resulting
    /// Attendance/AttendanceLog rows with where they came from (DeviceId)
    /// for audit/dashboard/simulator "Clear Test Data" purposes -
    /// AttendanceService itself has no concept of a biometric device.
    /// </summary>
    public class AttendanceProcessorService
    : IAttendanceProcessorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<AttendanceProcessorService> _logger;

        public AttendanceProcessorService(
            ApplicationDbContext db,
            IAttendanceService attendanceService,
            ILogger<AttendanceProcessorService> logger)
        {
            _db = db;
            _attendanceService = attendanceService;
            _logger = logger;
        }

        public async Task<bool>
            ProcessAttendanceAsync()
        {
            try
            {
                // Ordered by PunchTime so each employee's punches replay in
                // the sequence they actually happened - PunchOutAsync/
                // BreakInAsync/BreakOutAsync all validate against the
                // previous log's PunchType (e.g. "Break in not allowed"
                // unless the last log was Break out), so out-of-order replay
                // would spuriously fail real punches.
                var rawLogs =
                    await _db.BiometricAttendanceLogs
                    .Where(x => !x.IsProcessed)
                    .OrderBy(x => x.PunchTime)
                    .ToListAsync();

                foreach (var raw in rawLogs)
                {
                    var mapping =
                        await _db
                        .EmployeeBiometricMappings
                        .FirstOrDefaultAsync(x =>
                            x.BiometricEmployeeCode ==
                            raw.EmployeeCode &&
                            x.IsActive);

                    if (mapping == null)
                    {
                        _logger.LogWarning(
                            "Biometric punch skipped - no active Employee Biometric Mapping for device code {Code} (raw log {RawId}, device {DeviceId}). Create a mapping so future syncs pick this up.",
                            raw.EmployeeCode, raw.Id, raw.DeviceId);
                        continue;
                    }

                    var employee =
                        await _db.Employees
                        .FirstOrDefaultAsync(x =>
                            x.Id == mapping.EmployeeId);

                    if (employee == null)
                    {
                        _logger.LogWarning(
                            "Biometric punch skipped - mapped employee {EmployeeId} no longer exists (raw log {RawId}).",
                            mapping.EmployeeId, raw.Id);
                        continue;
                    }

                    var dto = new PunchRequestDto
                    {
                        EmployeeId = employee.Id,
                        PunchTime = raw.PunchTime,
                        DeviceType = "Biometric",
                        IsManual = false,
                        CreatedBy = string.IsNullOrEmpty(raw.CreatedBy) ? "BiometricSync" : raw.CreatedBy
                    };

                    bool applied;

                    switch (raw.PunchType)
                    {
                        case PunchType.In:
                            applied = await _attendanceService.PunchInAsync(dto);
                            break;

                        case PunchType.Out:
                            applied = await _attendanceService.PunchOutAsync(dto);
                            break;

                        case PunchType.BreakOut:
                            applied = await _attendanceService.BreakOutAsync(dto);
                            break;

                        case PunchType.BreakIn:
                            applied = await _attendanceService.BreakInAsync(dto);
                            break;

                        default:
                            _logger.LogWarning(
                                "Biometric punch skipped - unrecognised PunchType {PunchType} (raw log {RawId}).",
                                raw.PunchType, raw.Id);
                            applied = false;
                            break;
                    }

                    if (!applied)
                    {
                        // AttendanceService's Punch*/Break* methods reject
                        // out-of-sequence punches (e.g. two INs in a row, or
                        // an OUT with no open IN) rather than silently
                        // corrupting the day's attendance. Leave IsProcessed
                        // = false so this is retried on the next sync and
                        // visible via this warning for investigation -
                        // never silently dropped.
                        _logger.LogWarning(
                            "Biometric punch could not be applied to attendance - Employee {EmployeeId}, PunchType {PunchType}, PunchTime {PunchTime:o} (raw log {RawId}). Left unprocessed for retry.",
                            employee.Id, raw.PunchType, raw.PunchTime, raw.Id);
                        continue;
                    }

                    // Tag the Attendance/AttendanceLog row(s) that punch just
                    // produced with where it came from. AttendanceService has
                    // no concept of a biometric device, so this is done here
                    // as a follow-up enrichment rather than by touching its
                    // internals - never changes any of the values it already
                    // computed (shift/late/OT/hours/status).
                    var log = await _db.AttendanceLogs
                        .Where(x =>
                            x.EmployeeId == employee.Id &&
                            x.PunchTime == raw.PunchTime)
                        .OrderByDescending(x => x.CreatedOn)
                        .FirstOrDefaultAsync();

                    if (log != null)
                    {
                        log.DeviceId = raw.DeviceId;
                        log.BiometricCode = raw.EmployeeCode;

                        var attendance = await _db.Attendances
                            .FirstOrDefaultAsync(x => x.Id == log.AttendanceId);

                        if (attendance != null)
                        {
                            attendance.IsBiometricAttendance = true;
                            attendance.SourceDeviceId = raw.DeviceId;
                            attendance.ProcessedOn = DateTime.UtcNow;
                            attendance.ProcessedBy = "BiometricSync";
                        }
                    }

                    raw.IsProcessed = true;
                    raw.ProcessedOn = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessAttendanceAsync failed.");
                return false;
            }
        }

        public async Task<AttendanceProcessDto>
            CalculateAttendanceAsync(
                string employeeId,
                DateTime date)
        {
            try
            {
            var attendance =
                await _db.Attendances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.Date == date);

            if (attendance == null)
                return null;

            return new AttendanceProcessDto
            {
                EmployeeId = attendance.EmployeeId,
                AttendanceDate = attendance.Date,
                FirstIn = attendance.FirstIn,
                LastOut = attendance.LastOut,
                WorkingHours =
                    attendance.TotalWorkingHours,
                BreakHours =
                    attendance.BreakHours,
                OvertimeHours =
                    attendance.OvertimeHours,
                Status = attendance.Status
            };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
