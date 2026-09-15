-- ============================================================================
-- Performance indexes for the biometric attendance processing pipeline
-- (eSSL sync -> BiometricAttendanceLogs -> AttendanceProcessorService ->
-- AttendanceLogs/Attendances). Safe to run more than once (every statement
-- is guarded). Does not alter or remove any existing column/data - purely
-- additive index changes.
--
-- Run this once against the HRMS application database (the same database
-- BiometricAttendanceLogs/EmployeeBiometricMappings live in), alongside
-- "add AttendanceLog biometric link and indexes.sql" and EsslBulkStaging.sql
-- if that has not been run yet either.
-- ============================================================================

-- ---------------------------------------------------------------------
-- 1. BiometricAttendanceLogs: extend the existing (TenantId, IsProcessed)
--    index to also cover PunchTime, so AttendanceProcessorService's main
--    query - "WHERE IsProcessed = 0 ORDER BY PunchTime" - is satisfied by
--    the index itself (seek + already-sorted) instead of a scan followed
--    by a separate sort once the table grows past a trivial size.
-- ---------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed' AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
BEGIN
    DECLARE @hasPunchTime BIT = (
        SELECT CASE WHEN COUNT(*) = 0 THEN 0 ELSE 1 END
        FROM sys.index_columns ic
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE i.name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed'
          AND i.object_id = OBJECT_ID('dbo.BiometricAttendanceLogs')
          AND c.name = 'PunchTime'
    );

    IF @hasPunchTime = 0
        DROP INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed] ON [dbo].[BiometricAttendanceLogs];
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BiometricAttendanceLogs_Tenant_IsProcessed' AND object_id = OBJECT_ID('dbo.BiometricAttendanceLogs'))
    CREATE INDEX [IX_BiometricAttendanceLogs_Tenant_IsProcessed]
        ON [dbo].[BiometricAttendanceLogs]([TenantId], [IsProcessed], [PunchTime]);
GO

-- ---------------------------------------------------------------------
-- 2. EmployeeBiometricMappings: AttendanceProcessorService and
--    EsslAttendanceSyncService both preload "every active mapping" once
--    per run (WHERE IsActive = 1) rather than querying per punch - this
--    index makes that preload an index seek/scan instead of a full table
--    scan, and also covers any future direct BiometricEmployeeCode lookup.
-- ---------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EmployeeBiometricMappings_Code_IsActive' AND object_id = OBJECT_ID('dbo.EmployeeBiometricMappings'))
    CREATE INDEX [IX_EmployeeBiometricMappings_Code_IsActive]
        ON [dbo].[EmployeeBiometricMappings]([BiometricEmployeeCode], [IsActive]);
GO

-- Sanity check (uncomment to run manually):
-- SELECT COUNT(*) AS PendingBacklog FROM [dbo].[BiometricAttendanceLogs] WHERE [IsProcessed] = 0;
-- SELECT COUNT(*) AS ActiveMappings FROM [dbo].[EmployeeBiometricMappings] WHERE [IsActive] = 1;
