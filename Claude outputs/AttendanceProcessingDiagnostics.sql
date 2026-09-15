-- ============================================================================
-- eSSL Biometric Attendance Processing - Reconciliation Diagnostics
-- Run these against the live HRMS database (read-only, SELECT-only) to see
-- the EXACT breakdown behind "3595 raw rows -> only ~50 AttendanceLogs".
-- Run them AFTER redeploying the AttendanceProcessorService/AttendanceService
-- fix and re-running the eSSL sync (or the "Process Attendance" action) at
-- least once, so IsProcessed reflects the corrected logic.
-- ============================================================================

-- 1) Top-level counts -------------------------------------------------------
SELECT
    (SELECT COUNT(*) FROM BiometricAttendanceLogs)                                   AS TotalRawRecords,
    (SELECT COUNT(*) FROM BiometricAttendanceLogs WHERE IsProcessed = 1)             AS Processed,
    (SELECT COUNT(*) FROM BiometricAttendanceLogs WHERE IsProcessed = 0)             AS Unprocessed,
    (SELECT COUNT(*) FROM AttendanceLogs)                                            AS TotalAttendanceLogs,
    (SELECT COUNT(*) FROM Attendances)                                               AS TotalAttendances;

-- 2) Unmapped raw records - EmployeeCode has no ACTIVE EmployeeBiometricMapping,
--    normalized the same way BiometricEmployeeCodeNormalizer.Normalize does
--    (trim + upper) - this is the #1 suspect for a huge unprocessed backlog.
SELECT
    bal.EmployeeCode,
    COUNT(*) AS RawRecordCount,
    MIN(bal.PunchTime) AS FirstPunch,
    MAX(bal.PunchTime) AS LastPunch
FROM BiometricAttendanceLogs bal
WHERE bal.IsProcessed = 0
  AND NOT EXISTS (
        SELECT 1 FROM EmployeeBiometricMappings ebm
        WHERE ebm.IsActive = 1
          AND ebm.IsDeleted = 0
          AND UPPER(LTRIM(RTRIM(ebm.BiometricEmployeeCode))) = UPPER(LTRIM(RTRIM(bal.EmployeeCode)))
  )
GROUP BY bal.EmployeeCode
ORDER BY RawRecordCount DESC;

-- 3) Unprocessed records BY PunchType - if almost everything resolved to the
--    same PunchType (e.g. nearly all "In"), that points at
--    EsslAttendanceSyncService.ResolvePunchType's alternating fallback
--    (used when the eSSL source has no reliable Direction/AttDirection
--    token) rather than a mapping problem.
SELECT
    PunchType,
    COUNT(*) AS UnprocessedCount
FROM BiometricAttendanceLogs
WHERE IsProcessed = 0
GROUP BY PunchType
ORDER BY UnprocessedCount DESC;

-- 4) Per Employee+Day: how many raw punches vs how many actually became
--    AttendanceLogs. A day with many raw punches but 0-1 AttendanceLogs is
--    exactly the "rejected as out-of-sequence" pattern - cross-reference
--    against the ErrorLog table (module='Biometric Device Integration' /
--    feature='eSSL Attendance Processing') for the exact Reason text once
--    the fix has run.
SELECT
    bal.EmployeeCode,
    CAST(bal.PunchTime AS date) AS PunchDate,
    COUNT(*) AS RawPunchCount,
    SUM(CASE WHEN bal.IsProcessed = 1 THEN 1 ELSE 0 END) AS ProcessedCount,
    SUM(CASE WHEN bal.IsProcessed = 0 THEN 1 ELSE 0 END) AS UnprocessedCount
FROM BiometricAttendanceLogs bal
GROUP BY bal.EmployeeCode, CAST(bal.PunchTime AS date)
HAVING SUM(CASE WHEN bal.IsProcessed = 0 THEN 1 ELSE 0 END) > 0
ORDER BY RawPunchCount DESC;

-- 5) Employees with an active mapping but no DefaultShift/EmployeeShiftMapping
--    coverage for the sync window (ShiftId is NOT NULL at the DB level, so
--    this should normally return 0 rows - a non-empty result here means
--    legacy/pre-migration data is missing a shift and is worth a look).
SELECT e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.ShiftId
FROM Employees e
WHERE e.ShiftId IS NULL OR e.ShiftId = '';

-- 6) Recent ErrorLog entries from this fix's new reconciliation logging -
--    shows the exact per-run counters and any per-row failure surfaced to
--    ErrorLog (Failed > 0 case) after redeploying.
SELECT TOP 50 *
FROM ErrorLogs
WHERE FeatureName IN ('eSSL Attendance Processing', 'Biometric Attendance Processing')
ORDER BY ErrorTime DESC;
