using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// handle night shifts that cross midnight via GetAttendanceDate, apply
    /// GraceIn/GraceOutMinutes, compute TotalWorkingHours net of breaks,
    /// OvertimeHours, IsLate, IsEarlyExit and the Present/HalfDay/Absent
    /// status). So each raw punch is replayed through those exact same
    /// methods instead - the biometric path and the manual-punch path share
    /// one engine, per the "reuse the existing ERP attendance logic"
    /// requirement. This class's job is: resolve which raw punches map to
    /// which employee, replay them in order, tag the resulting
    /// Attendance/AttendanceLog rows with where they came from, and do all
    /// of that in a way that is batched, idempotent, and retry-safe:
    ///
    ///   - Batched: unprocessed rows are pulled BatchSize at a time (not the
    ///     whole table at once), with per-batch bulk lookups instead of a
    ///     query per punch.
    ///   - Idempotent: before replaying a raw punch, this checks whether an
    ///     AttendanceLog is already linked to it (AttendanceLog.
    ///     BiometricAttendanceLogId) - by BiometricAttendanceLogId first
    ///     (bulk-loaded per batch), falling back to an EmployeeId+PunchTime+
    ///     PunchType match for logs created before that link could be saved
    ///     (see the retry note below). If found, the raw log is just healed
    ///     (linked + marked processed) instead of being replayed again -
    ///     AttendanceService's own "Already punched in/out" guards would
    ///     otherwise make a genuine re-run silently get stuck as
    ///     unprocessed forever.
    ///   - Retry-safe: AttendanceService's Punch*/Break* methods call
    ///     SaveChangesAsync() themselves (on this same DbContext instance),
    ///     so a punch that was successfully applied is already durably
    ///     committed before this class even gets control back. This class
    ///     then does its own SaveChangesAsync per raw record (tag-back +
    ///     IsProcessed) - if THAT fails, the punch itself is not undone
    ///     (there is no ambient transaction spanning both, matching this
    ///     project's existing convention of one SaveChangesAsync call being
    ///     the unit of atomicity - see EsslAttendanceSyncService's identical
    ///     per-row SaveChangesAsync pattern), but the row stays
    ///     IsProcessed = false and the idempotency check above heals it on
    ///     the very next run without ever creating a duplicate.
    /// </summary>
    public class AttendanceProcessorService
    : IAttendanceProcessorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IAttendanceService _attendanceService;
        private readonly IErrorLogService _errorLogService;
        private readonly ILogger<AttendanceProcessorService> _logger;

        // How many unprocessed BiometricAttendanceLog rows are loaded into
        // memory at a time (spec: "do not load millions of biometric
        // records into memory"). A batch's tracked entities are cleared
        // from the DbContext once the batch finishes, bounding memory
        // regardless of total backlog size.
        private const int DefaultBatchSize = 500;

        // Hard ceiling on how many batches one ProcessAttendanceAsync()
        // call will drain, so a huge backlog can't turn one invocation into
        // an unbounded run - remaining rows are simply picked up by the
        // next scheduled/triggered call (eSSL sync cycle or ingest
        // endpoint), never lost or marked processed early.
        private const int MaxBatchesPerRun = 200;

        public AttendanceProcessorService(
            ApplicationDbContext db,
            IAttendanceService attendanceService,
            IErrorLogService errorLogService,
            ILogger<AttendanceProcessorService> logger)
        {
            _db = db;
            _attendanceService = attendanceService;
            _errorLogService = errorLogService;
            _logger = logger;
        }

        public Task<bool> ProcessAttendanceAsync() => ProcessAttendanceAsync(DefaultBatchSize);

        /// <summary>Overload exposing the batch size for tests/tuning - the interface keeps the parameterless signature every existing caller (eSSL sync, BiometricSyncController) already uses unchanged.</summary>
        public async Task<bool> ProcessAttendanceAsync(int batchSize)
        {
            if (batchSize < 1) batchSize = DefaultBatchSize;

            _logger.LogInformation("Biometric attendance processing started (batchSize={BatchSize}).", batchSize);

            int totalFound = 0, totalApplied = 0, totalSkippedUnmapped = 0, totalHealed = 0, totalFailed = 0, totalRejectedSequence = 0;

            try
            {
                // Active mappings rarely change and are small relative to
                // the punch backlog - load once for the whole run rather
                // than once per punch (was: one query per raw log).
                // ROOT-CAUSE FIX: keyed by BiometricEmployeeCodeNormalizer.Normalize
                // (trim + invariant uppercase) instead of the raw
                // BiometricEmployeeCode - see that class's remarks. This is
                // the stage where a mapping mismatch actually costs a
                // record: a row already sitting in BiometricAttendanceLogs
                // (successfully imported) that fails THIS lookup is left
                // IsProcessed=false forever and never becomes an Attendance
                // record - which is exactly the shape of "many raw eSSL
                // rows, far fewer Attendance rows" reported gaps like this
                // one.
                var mappingByCode = await _db.EmployeeBiometricMappings
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .GroupBy(x => BiometricEmployeeCodeNormalizer.Normalize(x.BiometricEmployeeCode))
                    .ToDictionaryAsync(g => g.Key, g => g.First());

                for (int batchNo = 0; batchNo < MaxBatchesPerRun; batchNo++)
                {
                    // Re-queried every iteration (not Skip/Take) - rows
                    // processed in the previous iteration are already
                    // IsProcessed = true and saved, so they naturally drop
                    // out of this Where clause without needing an offset.
                    var batch = await _db.BiometricAttendanceLogs
                        .Where(x => !x.IsProcessed)
                        .OrderBy(x => x.PunchTime)
                        .Take(batchSize)
                        .ToListAsync();

                    if (batch.Count == 0)
                        break;

                    totalFound += batch.Count;

                    var employeeIds = batch
                        .Select(r => mappingByCode.TryGetValue(BiometricEmployeeCodeNormalizer.Normalize(r.EmployeeCode), out var m) ? m.EmployeeId : null)
                        .Where(id => id != null)
                        .Distinct()
                        .ToList();

                    var employeesById = await _db.Employees
                        .AsNoTracking()
                        .Where(x => employeeIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id);

                    // Idempotency fast-path: which of THIS batch's raw log
                    // ids already have an AttendanceLog linked to them
                    // (e.g. left over from a run that applied the punch but
                    // failed before committing IsProcessed - see class
                    // remarks).
                    var batchIds = batch.Select(r => r.Id).ToList();

                    var alreadyLinked = await _db.AttendanceLogs
                        .Where(x => x.BiometricAttendanceLogId != null && batchIds.Contains(x.BiometricAttendanceLogId))
                        .ToDictionaryAsync(x => x.BiometricAttendanceLogId!);

                    foreach (var raw in batch)
                    {
                        try
                        {
                            if (alreadyLinked.TryGetValue(raw.Id, out var linkedLog))
                            {
                                // Already applied in a previous run - just
                                // ensure the raw record reflects that
                                // (heals a partial-failure state without
                                // ever re-calling AttendanceService, which
                                // would reject it as "already punched in/out").
                                HealAndMarkProcessed(raw, linkedLog);
                                totalHealed++;
                                await _db.SaveChangesAsync();
                                continue;
                            }

                            if (!mappingByCode.TryGetValue(BiometricEmployeeCodeNormalizer.Normalize(raw.EmployeeCode), out var mapping))
                            {
                                // Phase 8 requirement: full identifying detail
                                // every time, not just a count - SourceTable/
                                // DeviceTransactionId (which encodes the
                                // originating eSSL DeviceLogId) and PunchTime
                                // (LogDate) are on BiometricAttendanceLog
                                // itself, so no extra lookup is needed.
                                _logger.LogWarning(
                                    "Biometric punch skipped - no active Employee Biometric Mapping for device code {Code} (raw log {RawId}, SourceTable={SourceTable}, DeviceTransactionId={DeviceTransactionId}, LogDate={LogDate:o}, device {DeviceId}). Create a mapping so future syncs pick this up.",
                                    raw.EmployeeCode, raw.Id, raw.SourceTable, raw.DeviceTransactionId, raw.PunchTime, raw.DeviceId);
                                totalSkippedUnmapped++;
                                continue; // left unprocessed for retry, per spec section 12
                            }

                            if (!employeesById.TryGetValue(mapping.EmployeeId, out var employee))
                            {
                                _logger.LogWarning(
                                    "Biometric punch skipped - mapped employee {EmployeeId} no longer exists (raw log {RawId}).",
                                    mapping.EmployeeId, raw.Id);
                                totalSkippedUnmapped++;
                                continue;
                            }

                            // Fallback idempotency check for logs created
                            // before BiometricAttendanceLogId could be
                            // saved (see class remarks) - only hit for
                            // records the fast-path dictionary didn't
                            // already resolve, so this stays rare in the
                            // steady state.
                            var possiblyExisting = await _db.AttendanceLogs
                                .FirstOrDefaultAsync(x =>
                                    x.EmployeeId == employee.Id &&
                                    x.PunchTime == raw.PunchTime &&
                                    x.PunchType == raw.PunchType &&
                                    x.BiometricAttendanceLogId == null);

                            if (possiblyExisting != null)
                            {
                                HealAndMarkProcessed(raw, possiblyExisting);
                                totalHealed++;
                                await _db.SaveChangesAsync();
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
                                // AttendanceService's Punch*/Break* methods
                                // reject out-of-sequence punches (e.g. two
                                // INs in a row, or an OUT with no open IN)
                                // rather than silently corrupting the day's
                                // attendance. Leave IsProcessed = false so
                                // this is retried on the next sync and
                                // visible via this warning for investigation
                                // - never silently dropped.
                                //
                                // NOTE for reconciling a raw-row-count vs
                                // Attendance-row-count gap: this is the other
                                // major, entirely-expected reason those two
                                // numbers diverge even with mapping fully
                                // correct. A real device commonly logs
                                // several raw punches per employee per day
                                // (duplicate taps, IN/OUT/BreakOut/BreakIn),
                                // which legitimately collapse into far fewer
                                // Attendance rows once paired - "2671 raw
                                // DeviceLogs rows" was never expected to
                                // equal "2671 Attendance rows" even for a
                                // fully-mapped, fully-healthy sync.
                                // totalRejectedSequence is counted separately
                                // from totalSkippedUnmapped so the final
                                // summary line distinguishes "stuck because
                                // of a mapping problem" from "stuck because
                                // the device sent an out-of-sequence punch"
                                // - they need different fixes.
                                totalRejectedSequence++;
                                _logger.LogWarning(
                                    "Biometric punch could not be applied to attendance - Employee {EmployeeId}, PunchType {PunchType}, PunchTime {PunchTime:o} (raw log {RawId}, SourceTable={SourceTable}, DeviceTransactionId={DeviceTransactionId}). Left unprocessed for retry.",
                                    employee.Id, raw.PunchType, raw.PunchTime, raw.Id, raw.SourceTable, raw.DeviceTransactionId);
                                continue;
                            }

                            // Tag the AttendanceLog/Attendance row(s) that
                            // punch just produced with where it came from.
                            // AttendanceService has no concept of a
                            // biometric device, so this is done here as a
                            // follow-up enrichment rather than by touching
                            // its internals - never changes any of the
                            // values it already computed (shift/late/OT/
                            // hours/status).
                            var log = await _db.AttendanceLogs
                                .Where(x =>
                                    x.EmployeeId == employee.Id &&
                                    x.PunchTime == raw.PunchTime &&
                                    x.PunchType == raw.PunchType)
                                .OrderByDescending(x => x.CreatedOn)
                                .FirstOrDefaultAsync();

                            if (log == null)
                            {
                                // Should not happen (AttendanceService just
                                // reported success) - treat as a failure
                                // rather than silently marking processed
                                // with nothing to show for it.
                                _logger.LogError(
                                    "Biometric punch reported as applied but no matching AttendanceLog was found - Employee {EmployeeId}, PunchType {PunchType}, PunchTime {PunchTime:o} (raw log {RawId}). Left unprocessed for investigation.",
                                    employee.Id, raw.PunchType, raw.PunchTime, raw.Id);
                                totalFailed++;
                                continue;
                            }

                            log.BiometricAttendanceLogId = raw.Id;
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

                            raw.IsProcessed = true;
                            raw.ProcessedOn = DateTime.UtcNow;

                            // Saved per-record (not per-batch) so a failure
                            // on record N never rolls back record N-1's
                            // already-successful processed flag - matches
                            // this project's existing per-row SaveChanges
                            // convention (EsslAttendanceSyncService.SyncAsync).
                            await _db.SaveChangesAsync();

                            totalApplied++;
                        }
                        catch (Exception rowEx)
                        {
                            // One bad record must never abort the whole
                            // batch/run (spec section 19) - isolate, log,
                            // clear this record's tracked changes, move on.
                            // Leaves raw.IsProcessed = false for retry.
                            _db.ChangeTracker.Clear();

                            totalFailed++;

                            _logger.LogError(rowEx,
                                "Biometric attendance processing failed for raw log {RawId} (EmployeeCode {EmployeeCode}, PunchTime {PunchTime:o}, PunchType {PunchType}).",
                                raw.Id, raw.EmployeeCode, raw.PunchTime, raw.PunchType);

                            await _errorLogService.LogAsync(
                                rowEx,
                                module: "Attendance",
                                feature: "Biometric Attendance Processing",
                                controller: "AttendanceProcessorService",
                                action: "ProcessAttendanceAsync",
                                tenantId: raw.TenantId);
                        }
                    }

                    // Bound memory before the next batch - everything in
                    // this batch that needed saving has already been saved
                    // per-record above.
                    _db.ChangeTracker.Clear();
                }

                var reconciles = totalFound == (totalApplied + totalHealed + totalSkippedUnmapped + totalRejectedSequence + totalFailed);
                _logger.LogInformation(
                    "Biometric attendance processing completed. Found={Found}, Applied={Applied}, Healed={Healed}, SkippedUnmapped={SkippedUnmapped}, RejectedOutOfSequence={RejectedSequence}, Failed={Failed}, reconciles={Reconciles} (Found should == Applied+Healed+SkippedUnmapped+RejectedOutOfSequence+Failed).",
                    totalFound, totalApplied, totalHealed, totalSkippedUnmapped, totalRejectedSequence, totalFailed, reconciles);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ProcessAttendanceAsync failed.");

                await _errorLogService.LogAsync(
                    ex,
                    module: "Attendance",
                    feature: "Biometric Attendance Processing",
                    controller: "AttendanceProcessorService",
                    action: "ProcessAttendanceAsync");

                return false;
            }
        }

        private static void HealAndMarkProcessed(BiometricAttendanceLog raw, AttendanceLog log)
        {
            log.BiometricAttendanceLogId ??= raw.Id;
            log.DeviceId ??= raw.DeviceId;
            log.BiometricCode ??= raw.EmployeeCode;

            raw.IsProcessed = true;
            raw.ProcessedOn = DateTime.UtcNow;
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
