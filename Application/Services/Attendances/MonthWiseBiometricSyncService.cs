using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Attendances
{
    /// <summary>
    /// Standalone "Sync Biometric Attendance (Month Wise)" admin tool - see
    /// IMonthWiseBiometricSyncService.cs's remarks. This class NEVER
    /// references IEsslAttendanceSyncService, IHistoricalAttendanceSyncService,
    /// EsslAttendanceSyncState, any job queue, or AttendanceService.
    ///
    /// Pure C#/EF Core against ApplicationDbContext - no stored procedure, no
    /// raw ADO.NET (per explicit request to remove the SP that this class
    /// originally called, dbo.usp_SyncBiometricAttendance_MonthWise; the
    /// root-level "add MonthWise Biometric Attendance Sync stored
    /// procedure.sql" script is unused now and should be deleted from the
    /// repo). Replicates the SP's logic faithfully:
    /// 1. First punch (by PunchTime asc, then Id asc) per biometric-mapped
    ///    employee per calendar day within [FromDate, ToDate), from active
    ///    BiometricAttendanceLogs joined to active EmployeeBiometricMappings.
    /// 2. For each first-punch row: skip if already linked via an existing
    ///    AttendanceLogs.BiometricAttendanceLogId, or if an Attendance
    ///    already exists for that Employee+TenantId+calendar date; otherwise
    ///    insert a new Attendance + AttendanceLog and mark the source
    ///    BiometricAttendanceLog processed.
    /// 3. Returns the same counts/shape the SP used to return.
    /// </summary>
    public class MonthWiseBiometricSyncService : IMonthWiseBiometricSyncService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<MonthWiseBiometricSyncService> _logger;

        public MonthWiseBiometricSyncService(
            ApplicationDbContext db,
            ILogger<MonthWiseBiometricSyncService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<MonthWiseSyncResultDto> SyncAsync(
    MonthWiseSyncRequestDto request,
    string tenantId,
    CancellationToken ct = default)
        {
            // ============================================================
            // 1. VALIDATION
            // ============================================================

            if (request.Month < 1 || request.Month > 12)
                throw new ArgumentException(
                    "Month must be between 1 and 12.",
                    nameof(request));

            if (request.Year < 2000 || request.Year > 2100)
                throw new ArgumentException(
                    "Year must be between 2000 and 2100.",
                    nameof(request));

            if (string.IsNullOrWhiteSpace(request.CompanyId))
                throw new ArgumentException(
                    "Company is required.",
                    nameof(request));

            if (string.IsNullOrWhiteSpace(request.ShiftId))
                throw new ArgumentException(
                    "Shift is required.",
                    nameof(request));

            // ============================================================
            // 2. MONTH DATE RANGE
            // ============================================================

            var fromDate = new DateTime(
                request.Year,
                request.Month,
                1,
                0,
                0,
                0,
                DateTimeKind.Unspecified);

            var toDate = fromDate.AddMonths(1);

            var createdBy =
                string.IsNullOrWhiteSpace(request.CreatedBy)
                    ? "SYSTEM"
                    : request.CreatedBy;

            var status = (AttendanceStatus)request.Status;

            _logger.LogInformation(
                "Month Wise Biometric Sync: starting run for tenant {TenantId}, company {CompanyId}, shift {ShiftId}, {Year}-{Month:00}.",
                tenantId,
                request.CompanyId,
                request.ShiftId,
                request.Year,
                request.Month);

            // ============================================================
            // 3. LOAD ACTIVE BIOMETRIC EMPLOYEE MAPPINGS
            // ============================================================

            var mappings = await _db.EmployeeBiometricMappings
                .AsNoTracking()
                .Where(m =>
                    !m.IsDeleted &&
                    m.IsActive)
                .Select(m => new
                {
                    m.EmployeeId,
                    m.BiometricEmployeeCode
                })
                .ToListAsync(ct);

            var employeeIdByCode = mappings
                .GroupBy(m => m.BiometricEmployeeCode)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().EmployeeId);

            var mappedCodes = employeeIdByCode.Keys.ToList();

            if (mappedCodes.Count == 0)
            {
                return new MonthWiseSyncResultDto
                {
                    Year = request.Year,
                    Month = request.Month,
                    FromDate = fromDate,
                    ToDate = toDate,
                    AttendancesInserted = 0,
                    AttendanceLogsInserted = 0,
                    SkippedRecords = 0,
                    SyncStatus = "Completed"
                };
            }

            // ============================================================
            // 4. LOAD MONTH BIOMETRIC PUNCHES
            //
            // IMPORTANT:
            // Direction is intentionally NOT used.
            //
            // Device stores Direction = "in" for every punch.
            // Therefore:
            //
            // First punch of day = FirstIn
            // Last punch of day  = LastOut
            // ============================================================

            var punches = await _db.BiometricAttendanceLogs
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    x.IsActive &&
                    (x.TenantId == tenantId || x.TenantId == null) &&
                    x.PunchTime >= fromDate &&
                    x.PunchTime < toDate &&
                    mappedCodes.Contains(x.EmployeeCode))
                .Select(x => new
                {
                    x.Id,
                    x.EmployeeCode,
                    x.PunchTime,
                    x.DeviceId
                })
                .ToListAsync(ct);

            // ============================================================
            // 5. GROUP PUNCHES
            //
            // Employee + Calendar Date
            //
            // FirstIn = earliest punch
            // LastOut = latest punch
            // ============================================================

            var dailyPunches = punches
                .Select(p => new
                {
                    p.Id,
                    p.EmployeeCode,
                    p.PunchTime,
                    p.DeviceId,

                    EmployeeId =
                        employeeIdByCode[p.EmployeeCode],

                    Date =
                        p.PunchTime.Date
                })
                .GroupBy(p => new
                {
                    p.EmployeeId,
                    p.Date
                })
                .Select(g =>
                {
                    var ordered = g
                        .OrderBy(x => x.PunchTime)
                        .ThenBy(x => x.Id, StringComparer.Ordinal)
                        .ToList();

                    var first = ordered.First();

                    var last = ordered.Last();

                    return new
                    {
                        EmployeeId = g.Key.EmployeeId,

                        Date = g.Key.Date,

                        // ------------------------------------------------
                        // FIRST IN
                        // ------------------------------------------------

                        FirstInId =
                            first.Id,

                        FirstInEmployeeCode =
                            first.EmployeeCode,

                        FirstInTime =
                            first.PunchTime,

                        FirstInDeviceId =
                            first.DeviceId,

                        // ------------------------------------------------
                        // LAST OUT
                        // ------------------------------------------------

                        LastOutId =
                            ordered.Count > 1
                                ? last.Id
                                : null,

                        LastOutEmployeeCode =
                            ordered.Count > 1
                                ? last.EmployeeCode
                                : null,

                        LastOutTime =
                            ordered.Count > 1
                                ? last.PunchTime
                                : (DateTime?)null,

                        LastOutDeviceId =
                            ordered.Count > 1
                                ? last.DeviceId
                                : null,

                        TotalPunches =
                            ordered.Count
                    };
                })
                .ToList();

            // ============================================================
            // 6. NO DATA
            // ============================================================

            if (dailyPunches.Count == 0)
            {
                _logger.LogInformation(
                    "Month Wise Biometric Sync: no biometric punches found for tenant {TenantId}, {Year}-{Month:00}.",
                    tenantId,
                    request.Year,
                    request.Month);

                return new MonthWiseSyncResultDto
                {
                    Year = request.Year,
                    Month = request.Month,
                    FromDate = fromDate,
                    ToDate = toDate,

                    AttendancesInserted = 0,
                    AttendanceLogsInserted = 0,
                    SkippedRecords = 0,

                    SyncStatus = "Completed"
                };
            }

            // ============================================================
            // 7. GET EXISTING ATTENDANCES
            //
            // IMPORTANT:
            // We DO NOT skip existing records anymore.
            //
            // Existing biometric Attendance will be UPDATED.
            // ============================================================

            var candidateEmployeeIds = dailyPunches
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToList();

            var existingAttendances =
                await _db.Attendances
                    .Where(a =>
                        a.TenantId == tenantId &&
                        candidateEmployeeIds.Contains(a.EmployeeId) &&
                        a.Date >= fromDate &&
                        a.Date < toDate)
                    .ToListAsync(ct);

            var attendanceByEmployeeDate =
                existingAttendances
                    .GroupBy(x => new
                    {
                        x.EmployeeId,
                        Date = x.Date.Date
                    })
                    .ToDictionary(
                        g => (
                            g.Key.EmployeeId,
                            g.Key.Date),
                        g => g.First());

            // ============================================================
            // 8. EXISTING ATTENDANCE LOGS
            //
            // Load logs for existing attendance records so that we can
            // avoid unnecessary duplicate AttendanceLog rows.
            // ============================================================

            var existingAttendanceIds =
                existingAttendances
                    .Select(x => x.Id)
                    .ToList();

            var existingAttendanceLogs =
                existingAttendanceIds.Count == 0
                    ? new List<AttendanceLog>()
                    : await _db.AttendanceLogs
                        .Where(x =>
                            existingAttendanceIds.Contains(
                                x.AttendanceId))
                        .ToListAsync(ct);

            // ============================================================
            // 9. CREATE LOOKUPS
            // ============================================================

            var attendanceLogByBiometricId =
                existingAttendanceLogs
                    .Where(x =>
                        x.BiometricAttendanceLogId != null)
                    .GroupBy(x =>
                        x.BiometricAttendanceLogId!)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());

            // ============================================================
            // 10. INSERT / UPDATE COUNTERS
            // ============================================================

            var attendancesInserted = 0;

            var attendancesUpdated = 0;

            var attendanceLogsInserted = 0;

            var attendanceLogsUpdated = 0;

            var skippedRecords = 0;

            var now = DateTime.UtcNow;

            // ============================================================
            // 11. PROCESS EACH EMPLOYEE + DATE
            // ============================================================

            foreach (var punch in dailyPunches)
            {
                // --------------------------------------------------------
                // FIND EXISTING ATTENDANCE
                // --------------------------------------------------------

                attendanceByEmployeeDate.TryGetValue(
                    (
                        punch.EmployeeId,
                        punch.Date),
                    out var attendance);

                // --------------------------------------------------------
                // CALCULATE WORKING HOURS
                // --------------------------------------------------------

                decimal totalWorkingHours = 0;

                if (punch.LastOutTime.HasValue)
                {
                    var duration =
                        punch.LastOutTime.Value -
                        punch.FirstInTime;

                    if (duration.TotalMinutes > 0)
                    {
                        totalWorkingHours =
                            (decimal)duration.TotalHours;
                    }
                }

                // ========================================================
                // 12. CREATE NEW ATTENDANCE
                // ========================================================

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        TenantId = tenantId,

                        CompanyId =
                            request.CompanyId,

                        BranchId =
                            request.BranchId,

                        EmployeeId =
                            punch.EmployeeId,

                        Date =
                            punch.Date,

                        ShiftId =
                            request.ShiftId,

                        FirstIn =
                            punch.FirstInTime,

                        LastOut =
                            punch.LastOutTime,

                        TotalWorkingHours =
                            totalWorkingHours,

                        BreakHours = 0,

                        OvertimeHours = 0,

                        Status =
                            status,

                        IsLate = false,

                        IsEarlyExit = false,

                        Remarks =
                            "Attendance synchronized from biometric device",

                        IsManualEntry = false,

                        IsBiometricAttendance = true,

                        ProcessedOn = now,

                        ProcessedBy = createdBy,

                        SourceDeviceId =
                            punch.FirstInDeviceId,

                        IsActive = true,

                        CreatedOn = now,

                        CreatedBy = createdBy
                    };

                    attendance.Id =
                        IDManager.GetNewId(attendance);

                    _db.Attendances.Add(attendance);

                    attendanceByEmployeeDate[
                        (
                            punch.EmployeeId,
                            punch.Date)] =
                        attendance;

                    attendancesInserted++;
                }
                else
                {
                    // ====================================================
                    // IMPORTANT FIX
                    //
                    // Existing biometric attendance is UPDATED.
                    //
                    // This fixes previously incorrect records such as:
                    //
                    // 13-May -> FirstIn incorrectly 18:56
                    // 19-May -> FirstIn incorrectly 18:36
                    // ====================================================

                    attendance.FirstIn =
                        punch.FirstInTime;

                    attendance.LastOut =
                        punch.LastOutTime;

                    attendance.TotalWorkingHours =
                        totalWorkingHours;

                    attendance.CompanyId =
                        request.CompanyId;

                    attendance.BranchId =
                        request.BranchId;

                    attendance.ShiftId =
                        request.ShiftId;

                    attendance.Status =
                        status;

                    attendance.IsBiometricAttendance =
                        true;

                    attendance.SourceDeviceId =
                        punch.FirstInDeviceId;

                    attendance.ProcessedOn =
                        now;

                    attendance.ProcessedBy =
                        createdBy;

                    attendance.ModifiedOn =
                        now;

                    attendance.ModifiedBy =
                        createdBy;

                    attendance.IsActive =
                        true;

                    attendancesUpdated++;
                }

                // ========================================================
                // 13. FIRST IN ATTENDANCE LOG
                // ========================================================

                if (attendanceLogByBiometricId.TryGetValue(
                        punch.FirstInId,
                        out var firstInLog))
                {
                    // ----------------------------------------------------
                    // UPDATE EXISTING FIRST-IN LOG
                    // ----------------------------------------------------

                    firstInLog.AttendanceId =
                        attendance.Id;

                    firstInLog.EmployeeId =
                        punch.EmployeeId;

                    firstInLog.PunchTime =
                        punch.FirstInTime;

                    firstInLog.PunchType =
                        PunchType.In;

                    firstInLog.DeviceType =
                        "Biometric";

                    firstInLog.DeviceId =
                        punch.FirstInDeviceId;

                    firstInLog.BiometricCode =
                        punch.FirstInEmployeeCode;

                    firstInLog.TenantId =
                        tenantId;

                    firstInLog.IsManual =
                        false;

                    firstInLog.IsActive =
                        true;

                    firstInLog.ModifiedOn =
                        now;

                    firstInLog.ModifiedBy =
                        createdBy;

                    attendanceLogsUpdated++;
                }
                else
                {
                    // ----------------------------------------------------
                    // CREATE FIRST-IN LOG
                    // ----------------------------------------------------

                    firstInLog = new AttendanceLog
                    {
                        AttendanceId =
                            attendance.Id,

                        EmployeeId =
                            punch.EmployeeId,

                        PunchTime =
                            punch.FirstInTime,

                        PunchType =
                            PunchType.In,

                        DeviceType =
                            "Biometric",

                        IsManual = false,

                        DeviceId =
                            punch.FirstInDeviceId,

                        BiometricCode =
                            punch.FirstInEmployeeCode,

                        BiometricAttendanceLogId =
                            punch.FirstInId,

                        TenantId =
                            tenantId,

                        IsActive = true,

                        CreatedOn =
                            now,

                        CreatedBy =
                            createdBy
                    };

                    firstInLog.Id =
                        IDManager.GetNewId(firstInLog);

                    _db.AttendanceLogs.Add(
                        firstInLog);

                    attendanceLogsInserted++;

                    attendanceLogByBiometricId[
                        punch.FirstInId] =
                        firstInLog;
                }

                // ========================================================
                // 14. LAST OUT ATTENDANCE LOG
                // ========================================================

                if (!string.IsNullOrWhiteSpace(
                        punch.LastOutId) &&
                    punch.LastOutTime.HasValue)
                {
                    if (attendanceLogByBiometricId.TryGetValue(
                            punch.LastOutId,
                            out var lastOutLog))
                    {
                        // ------------------------------------------------
                        // UPDATE EXISTING LAST-OUT LOG
                        // ------------------------------------------------

                        lastOutLog.AttendanceId =
                            attendance.Id;

                        lastOutLog.EmployeeId =
                            punch.EmployeeId;

                        lastOutLog.PunchTime =
                            punch.LastOutTime.Value;

                        lastOutLog.PunchType =
                            PunchType.Out;

                        lastOutLog.DeviceType =
                            "Biometric";

                        lastOutLog.DeviceId =
                            punch.LastOutDeviceId;

                        lastOutLog.BiometricCode =
                            punch.LastOutEmployeeCode;

                        lastOutLog.TenantId =
                            tenantId;

                        lastOutLog.IsManual =
                            false;

                        lastOutLog.IsActive =
                            true;

                        lastOutLog.ModifiedOn =
                            now;

                        lastOutLog.ModifiedBy =
                            createdBy;

                        attendanceLogsUpdated++;
                    }
                    else
                    {
                        // ------------------------------------------------
                        // CREATE LAST-OUT LOG
                        // ------------------------------------------------

                        lastOutLog = new AttendanceLog
                        {
                            AttendanceId =
                                attendance.Id,

                            EmployeeId =
                                punch.EmployeeId,

                            PunchTime =
                                punch.LastOutTime.Value,

                            PunchType =
                                PunchType.Out,

                            DeviceType =
                                "Biometric",

                            IsManual = false,

                            DeviceId =
                                punch.LastOutDeviceId,

                            BiometricCode =
                                punch.LastOutEmployeeCode,

                            BiometricAttendanceLogId =
                                punch.LastOutId,

                            TenantId =
                                tenantId,

                            IsActive = true,

                            CreatedOn =
                                now,

                            CreatedBy =
                                createdBy
                        };

                        lastOutLog.Id =
                            IDManager.GetNewId(lastOutLog);

                        _db.AttendanceLogs.Add(
                            lastOutLog);

                        attendanceLogsInserted++;

                        attendanceLogByBiometricId[
                            punch.LastOutId] =
                            lastOutLog;
                    }
                }

                // ========================================================
                // 15. MARK FIRST + LAST BIOMETRIC LOG AS PROCESSED
                // ========================================================

                var biometricIds =
                    new List<string>
                    {
                punch.FirstInId
                    };

                if (!string.IsNullOrWhiteSpace(
                        punch.LastOutId))
                {
                    biometricIds.Add(
                        punch.LastOutId);
                }

                var biometricLogsToMark =
                    await _db.BiometricAttendanceLogs
                        .Where(x =>
                            biometricIds.Contains(x.Id))
                        .ToListAsync(ct);

                foreach (var biometricLog
                         in biometricLogsToMark)
                {
                    biometricLog.IsProcessed =
                        true;

                    biometricLog.ProcessedOn =
                        now;

                    biometricLog.ModifiedOn =
                        now;

                    biometricLog.ModifiedBy =
                        createdBy;
                }
            }

            // ============================================================
            // 16. SAVE USING EXECUTION STRATEGY
            // ============================================================

            var strategy =
                _db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _db.Database.BeginTransactionAsync(
                        IsolationLevel.ReadCommitted,
                        ct);

                try
                {
                    await _db.SaveChangesAsync(ct);

                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);

                    _db.ChangeTracker.Clear();

                    throw;
                }
            });

            // ============================================================
            // 17. FINAL RESULT
            // ============================================================

            var result = new MonthWiseSyncResultDto
            {
                Year =
                    request.Year,

                Month =
                    request.Month,

                FromDate =
                    fromDate,

                ToDate =
                    toDate,

                AttendancesInserted =
                    attendancesInserted,

                AttendanceLogsInserted =
                    attendanceLogsInserted,

                SkippedRecords =
                    skippedRecords,

                SyncStatus =
                    "Completed"
            };

            // ============================================================
            // 18. LOG RESULT
            // ============================================================

            _logger.LogInformation(
                "Month Wise Biometric Sync completed. " +
                "Tenant={TenantId}, " +
                "Year={Year}, " +
                "Month={Month:00}, " +
                "AttendanceInserted={AttendanceInserted}, " +
                "AttendanceUpdated={AttendanceUpdated}, " +
                "AttendanceLogsInserted={AttendanceLogsInserted}, " +
                "AttendanceLogsUpdated={AttendanceLogsUpdated}, " +
                "Skipped={SkippedRecords}.",

                tenantId,

                request.Year,

                request.Month,

                attendancesInserted,

                attendancesUpdated,

                attendanceLogsInserted,

                attendanceLogsUpdated,

                skippedRecords);

            return result;
        }

        //public async Task<MonthWiseSyncResultDto> SyncAsync(MonthWiseSyncRequestDto request, string tenantId, CancellationToken ct = default)
        //{
        //    if (request.Month < 1 || request.Month > 12)
        //        throw new ArgumentException("Month must be between 1 and 12.", nameof(request));

        //    if (request.Year < 2000 || request.Year > 2100)
        //        throw new ArgumentException("Year must be between 2000 and 2100.", nameof(request));

        //    if (string.IsNullOrWhiteSpace(request.CompanyId))
        //        throw new ArgumentException("Company is required.", nameof(request));

        //    if (string.IsNullOrWhiteSpace(request.ShiftId))
        //        throw new ArgumentException("Shift is required.", nameof(request));

        //    var fromDate = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        //    var toDate = fromDate.AddMonths(1); // exclusive upper bound, matches the old SP's own window

        //    var createdBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "SYSTEM" : request.CreatedBy;
        //    var status = (AttendanceStatus)request.Status;

        //    _logger.LogInformation(
        //        "Month Wise Biometric Sync: starting run for tenant {TenantId}, company {CompanyId}, shift {ShiftId}, {Year}-{Month:00}.",
        //        tenantId, request.CompanyId, request.ShiftId, request.Year, request.Month);

        //    // Pull the whole month's active, mapped biometric punches into
        //    // memory - bounded, admin-triggered, single-month/single-tenant
        //    // scope (comparable to the Historical Sync feature's batching,
        //    // not the unbounded eSSL firehose) - then compute "first punch
        //    // per employee per day" and the two idempotency checks in C#
        //    // rather than forcing ROW_NUMBER()-style logic through EF-to-SQL
        //    // translation, matching HistoricalAttendanceSyncService's
        //    // materialize-then-process pattern.
        //    var mappings = await _db.EmployeeBiometricMappings.AsNoTracking()
        //        .Where(m => !m.IsDeleted && m.IsActive)
        //        .Select(m => new { m.EmployeeId, m.BiometricEmployeeCode })
        //        .ToListAsync(ct);

        //    var employeeIdByCode = mappings
        //        .GroupBy(m => m.BiometricEmployeeCode)
        //        .ToDictionary(g => g.Key, g => g.First().EmployeeId);

        //    var mappedCodes = employeeIdByCode.Keys.ToList();

        //    var punches = await _db.BiometricAttendanceLogs.AsNoTracking()
        //        .Where(x => !x.IsDeleted && x.IsActive &&
        //                    (x.TenantId == tenantId || x.TenantId == null) &&
        //                    x.PunchTime >= fromDate && x.PunchTime < toDate &&
        //                    mappedCodes.Contains(x.EmployeeCode))
        //        .Select(x => new
        //        {
        //            x.Id,
        //            x.EmployeeCode,
        //            x.PunchTime,
        //            x.DeviceId
        //        })
        //        .ToListAsync(ct);

        //    // First punch per employee per calendar day: order by PunchTime
        //    // asc, tie-broken by Id asc (matches the SP's ORDER BY), take
        //    // the first row of each (employee, date) group.
        //    var firstPunches = punches
        //        .Select(p => new
        //        {
        //            p.Id,
        //            p.EmployeeCode,
        //            p.PunchTime,
        //            p.DeviceId,
        //            EmployeeId = employeeIdByCode[p.EmployeeCode],
        //            Date = p.PunchTime.Date
        //        })
        //        .GroupBy(p => new { p.EmployeeId, p.Date })
        //        .Select(g => g.OrderBy(p => p.PunchTime).ThenBy(p => p.Id, StringComparer.Ordinal).First())
        //        .ToList();

        //    var skippedRecords = 0;

        //    if (firstPunches.Count == 0)
        //    {
        //        _logger.LogInformation(
        //            "Month Wise Biometric Sync: no eligible biometric punches found for tenant {TenantId}, {Year}-{Month:00}.",
        //            tenantId, request.Year, request.Month);

        //        return new MonthWiseSyncResultDto
        //        {
        //            Year = request.Year,
        //            Month = request.Month,
        //            FromDate = fromDate,
        //            ToDate = toDate,
        //            AttendancesInserted = 0,
        //            AttendanceLogsInserted = 0,
        //            SkippedRecords = 0,
        //            SyncStatus = "Completed"
        //        };
        //    }

        //    // "Already linked" check - one set-membership query for the
        //    // whole batch of candidate biometric log ids, not one query per
        //    // row.
        //    var candidateBiometricLogIds = firstPunches.Select(p => p.Id).ToList();
        //    var alreadyLinkedIds = (await _db.AttendanceLogs.AsNoTracking()
        //            .Where(x => x.BiometricAttendanceLogId != null && candidateBiometricLogIds.Contains(x.BiometricAttendanceLogId!))
        //            .Select(x => x.BiometricAttendanceLogId!)
        //            .ToListAsync(ct))
        //        .ToHashSet();

        //    // "Attendance already exists for Employee+Date" check - one
        //    // query for the whole batch's date range, not N+1.
        //    //
        //    // Deliberately NOT filtered on IsDeleted: UX_Attendances_Employee_Date_Tenant
        //    // (the real unique index backing dbo.Attendances - not modeled anywhere
        //    // in this codebase's Fluent API/migrations, so it was added directly
        //    // against the database) is unconditional across soft-deleted rows,
        //    // same as the sibling "already linked" check on AttendanceLogs right
        //    // above, and same as IX_AttendanceLogs_BiometricAttendanceLogId's own
        //    // filter (only "IS NOT NULL", never IsDeleted). A soft-deleted
        //    // Attendance row for this Employee+Date+TenantId still occupies that
        //    // unique index and blocks a fresh insert at the DB level even though
        //    // it is invisible to normal (non-deleted) queries - filtering this
        //    // check on IsDeleted, as an earlier version of this method did, let
        //    // exactly that case slip past the C# pre-check and fail later inside
        //    // SaveChangesAsync with "Cannot insert duplicate key row in object
        //    // 'dbo.Attendances' with unique index 'UX_Attendances_Employee_Date_Tenant'".
        //    var candidateEmployeeIds = firstPunches.Select(p => p.EmployeeId).Distinct().ToList();
        //    var existingAttendanceKeys = (await _db.Attendances.AsNoTracking()
        //            .Where(a => a.TenantId == tenantId &&
        //                        candidateEmployeeIds.Contains(a.EmployeeId) &&
        //                        a.Date >= fromDate && a.Date < toDate)
        //            .Select(a => new { a.EmployeeId, a.Date })
        //            .ToListAsync(ct))
        //        .Select(a => (a.EmployeeId, a.Date.Date))
        //        .ToHashSet();

        //    var newAttendances = new List<Attendance>();
        //    var newAttendanceLogs = new List<AttendanceLog>();
        //    var processedBiometricLogIds = new List<string>();
        //    var now = DateTime.UtcNow;

        //    foreach (var punch in firstPunches)
        //    {
        //        if (alreadyLinkedIds.Contains(punch.Id))
        //        {
        //            skippedRecords++;
        //            continue;
        //        }

        //        if (existingAttendanceKeys.Contains((punch.EmployeeId, punch.Date)))
        //        {
        //            skippedRecords++;
        //            continue;
        //        }

        //        var attendance = new Attendance
        //        {
        //            TenantId = tenantId,
        //            CompanyId = request.CompanyId,
        //            BranchId = request.BranchId,
        //            EmployeeId = punch.EmployeeId,
        //            Date = punch.Date,
        //            ShiftId = request.ShiftId,
        //            FirstIn = punch.PunchTime,
        //            LastOut = null,
        //            TotalWorkingHours = 0,
        //            BreakHours = 0,
        //            OvertimeHours = 0,
        //            Status = status,
        //            IsLate = false,
        //            IsEarlyExit = false,
        //            Remarks = "Attendance synchronized from biometric device",
        //            IsManualEntry = false,
        //            IsBiometricAttendance = true,
        //            ProcessedOn = now,
        //            ProcessedBy = createdBy,
        //            SourceDeviceId = punch.DeviceId,
        //            IsActive = true,
        //            CreatedOn = now,
        //            CreatedBy = createdBy
        //        };
        //        attendance.Id = IDManager.GetNewId(attendance);

        //        var attendanceLog = new AttendanceLog
        //        {
        //            AttendanceId = attendance.Id,
        //            EmployeeId = punch.EmployeeId,
        //            PunchTime = punch.PunchTime,
        //            PunchType = PunchType.In,
        //            DeviceType = "Biometric",
        //            IsManual = false,
        //            DeviceId = punch.DeviceId,
        //            BiometricCode = punch.EmployeeCode,
        //            BiometricAttendanceLogId = punch.Id,
        //            TenantId = tenantId,
        //            IsActive = true,
        //            CreatedOn = now,
        //            CreatedBy = createdBy
        //        };
        //        attendanceLog.Id = IDManager.GetNewId(attendanceLog);

        //        newAttendances.Add(attendance);
        //        newAttendanceLogs.Add(attendanceLog);
        //        processedBiometricLogIds.Add(punch.Id);
        //    }

        //    var attendancesInserted = newAttendances.Count;
        //    var attendanceLogsInserted = newAttendanceLogs.Count;

        //    if (attendancesInserted > 0)
        //    {
        //        // EnableRetryOnFailure (SqlServerRetryingExecutionStrategy) is
        //        // configured on this DbContext, so a manual
        //        // Database.BeginTransactionAsync(...) MUST run inside
        //        // Database.CreateExecutionStrategy().ExecuteAsync(...) - a bare
        //        // "using var transaction = await BeginTransactionAsync(...)"
        //        // throws "SqlServerRetryingExecutionStrategy does not support
        //        // user-initiated transactions" the moment anything inside it
        //        // touches the database. Whole run is one atomic transaction
        //        // (all-or-nothing for this sync), same pattern as
        //        // HistoricalAttendanceSyncService.ClaimAndCreateJobAsync.
        //        var strategy = _db.Database.CreateExecutionStrategy();
        //        var actuallyProcessedIds = new List<string>();

        //        await strategy.ExecuteAsync(async () =>
        //        {
        //            actuallyProcessedIds.Clear();

        //            using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        //            try
        //            {
        //                _db.Attendances.AddRange(newAttendances);
        //                _db.AttendanceLogs.AddRange(newAttendanceLogs);

        //                var biometricLogsToMark = await _db.BiometricAttendanceLogs
        //                    .Where(x => processedBiometricLogIds.Contains(x.Id))
        //                    .ToListAsync(ct);

        //                foreach (var log in biometricLogsToMark)
        //                {
        //                    log.IsProcessed = true;
        //                    log.ProcessedOn = now;
        //                    log.ModifiedOn = now;
        //                    log.ModifiedBy = createdBy;
        //                }

        //                await _db.SaveChangesAsync(ct);
        //                await transaction.CommitAsync(ct);
        //                actuallyProcessedIds.AddRange(processedBiometricLogIds);
        //            }
        //            catch (DbUpdateException ex) when (IsAttendanceUniqueConstraintViolation(ex))
        //            {
        //                // Defense-in-depth, not the primary fix: the batch
        //                // pre-checks above already exclude every row that
        //                // conflicted with data as of the start of this run, so
        //                // reaching here means a genuine race - e.g. a second
        //                // admin running the same month/company/shift sync (or
        //                // the eSSL/Historical sync pipelines) concurrently and
        //                // winning the insert for one of these rows between our
        //                // pre-check and this SaveChangesAsync. Roll back the
        //                // failed all-or-nothing bulk attempt and redo it
        //                // row-by-row so the one (or few) rows that lost the
        //                // race are skipped instead of discarding every
        //                // legitimate insert in this batch.
        //                _logger.LogWarning(ex,
        //                    "Month Wise Biometric Sync: bulk insert hit a unique-constraint conflict for tenant {TenantId}, {Year}-{Month:00} - falling back to row-by-row insert.",
        //                    tenantId, request.Year, request.Month);

        //                await transaction.RollbackAsync(ct);
        //                _db.ChangeTracker.Clear();

        //                for (var i = 0; i < newAttendances.Count; i++)
        //                {
        //                    using var rowTransaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        //                    try
        //                    {
        //                        _db.Attendances.Add(newAttendances[i]);
        //                        _db.AttendanceLogs.Add(newAttendanceLogs[i]);

        //                        var biometricLog = await _db.BiometricAttendanceLogs
        //                            .FirstOrDefaultAsync(x => x.Id == processedBiometricLogIds[i], ct);

        //                        if (biometricLog != null)
        //                        {
        //                            biometricLog.IsProcessed = true;
        //                            biometricLog.ProcessedOn = now;
        //                            biometricLog.ModifiedOn = now;
        //                            biometricLog.ModifiedBy = createdBy;
        //                        }

        //                        await _db.SaveChangesAsync(ct);
        //                        await rowTransaction.CommitAsync(ct);
        //                        actuallyProcessedIds.Add(processedBiometricLogIds[i]);
        //                    }
        //                    catch (DbUpdateException rowEx) when (IsAttendanceUniqueConstraintViolation(rowEx))
        //                    {
        //                        await rowTransaction.RollbackAsync(ct);
        //                        _db.ChangeTracker.Clear();
        //                        skippedRecords++;
        //                    }
        //                }
        //            }
        //        });

        //        if (actuallyProcessedIds.Count != newAttendances.Count)
        //        {
        //            attendancesInserted = actuallyProcessedIds.Count;
        //            attendanceLogsInserted = actuallyProcessedIds.Count;
        //        }
        //    }

        //    var result = new MonthWiseSyncResultDto
        //    {
        //        Year = request.Year,
        //        Month = request.Month,
        //        FromDate = fromDate,
        //        ToDate = toDate,
        //        AttendancesInserted = attendancesInserted,
        //        AttendanceLogsInserted = attendanceLogsInserted,
        //        SkippedRecords = skippedRecords,
        //        SyncStatus = "Completed"
        //    };

        //    _logger.LogInformation(
        //        "Month Wise Biometric Sync: finished for tenant {TenantId}, {Year}-{Month:00}. AttendancesInserted={AttendancesInserted}, AttendanceLogsInserted={AttendanceLogsInserted}, SkippedRecords={SkippedRecords}, Status={Status}.",
        //        tenantId, result.Year, result.Month, result.AttendancesInserted, result.AttendanceLogsInserted, result.SkippedRecords, result.SyncStatus);

        //    return result;
        //}

        // True only for the two specific unique-constraint violations this
        // method's own pre-checks are meant to prevent (SQL Server error 2627
        // = violation of a unique index/constraint), identified by the real
        // index names so an unrelated DbUpdateException (a genuine data
        // problem elsewhere) is never mistaken for a safe-to-skip race and
        // silently swallowed.
        private static bool IsAttendanceUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx &&
                   sqlEx.Number == 2627 &&
                   (sqlEx.Message.IndexOf("UX_Attendances_Employee_Date_Tenant", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sqlEx.Message.IndexOf("IX_AttendanceLogs_BiometricAttendanceLogId", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
