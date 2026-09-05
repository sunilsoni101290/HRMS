-- eSSL eTimeTrackLite1 direct-SQL attendance integration - monthly
-- DeviceLogs_M_YYYY partition table support. Schema changes against the
-- HRMS/ERP database (ApplicationDbContext) ONLY - this script never touches
-- etimetracklite1 (that database stays read-only/SELECT-only for this
-- integration, and this change adds no new columns or objects there either -
-- monthly table DISCOVERY is done purely via sys.tables/sys.schemas
-- metadata queries at runtime, in EsslAttendanceDataSource.
--
-- Idempotent - safe to re-run. Mirrors this repo's existing
-- "add EsslAttendanceSyncState table and BiometricSyncLog columns.sql" /
-- "add EsslIntegrationSettings table.sql" style. If you have the .NET SDK
-- available, `dotnet ef migrations add AddEsslMonthlyDeviceLogTableSupport
-- --project Infrastructure --startup-project API` should produce an
-- equivalent migration from the entity changes already made to
-- Domain/Entities/BiometricDevice.cs (BiometricAttendanceLog.SourceTable/
-- DownloadDate, BiometricSyncLog.SourceTables) and
-- Domain/Entities/EsslAttendanceSyncState.cs (LastProcessedSourceTable) -
-- this script is the ready-to-run equivalent for environments without the
-- SDK on hand (see Infrastructure/Migrations/20260901000000_AddEsslMonthlyDeviceLogTableSupport.cs
-- for the migration-history-tracked copy of these same statements).

-- =====================================================================
-- 1) BiometricAttendanceLogs - per-row audit trail for WHICH physical
--    eTimeTrackLite1 table an eSSL-sourced punch came from, and when
--    eTimeTrackLite1 downloaded it (vs. LogDate/PunchTime, the actual punch
--    time - see EsslDeviceLogRaw's remarks for why the two must never be
--    confused). NULL for every non-eSSL row (agent-pushed devices,
--    manual/web punches).
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricAttendanceLogs')
BEGIN
    RAISERROR('Table dbo.BiometricAttendanceLogs does not exist - the Biometric Device feature must be deployed before this script.', 16, 1);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'SourceTable')
        ALTER TABLE [dbo].[BiometricAttendanceLogs] ADD [SourceTable] NVARCHAR(60) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricAttendanceLogs') AND name = 'DownloadDate')
        ALTER TABLE [dbo].[BiometricAttendanceLogs] ADD [DownloadDate] DATETIME2 NULL;
END
GO

-- =====================================================================
-- 2) BiometricSyncLogs - which physical tables a given "EsslDbPull" run
--    actually scanned (audit/troubleshooting only - see
--    EsslAttendanceSyncService.SyncAsync).
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BiometricSyncLogs')
BEGIN
    RAISERROR('Table dbo.BiometricSyncLogs does not exist - run the earlier eSSL migration scripts first.', 16, 1);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BiometricSyncLogs') AND name = 'SourceTables')
        ALTER TABLE [dbo].[BiometricSyncLogs] ADD [SourceTables] NVARCHAR(500) NULL;
END
GO

-- =====================================================================
-- 3) EsslAttendanceSyncStates - informational/audit column recording which
--    physical table LastProcessedDeviceLogId came from. NEVER used as the
--    basis for the next run's query window or for duplicate detection (see
--    the column's XML doc comment on the entity) - purely diagnostic.
-- =====================================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EsslAttendanceSyncStates')
BEGIN
    RAISERROR('Table dbo.EsslAttendanceSyncStates does not exist - run the earlier eSSL migration scripts first.', 16, 1);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.EsslAttendanceSyncStates') AND name = 'LastProcessedSourceTable')
        ALTER TABLE [dbo].[EsslAttendanceSyncStates] ADD [LastProcessedSourceTable] NVARCHAR(60) NULL;
END
GO

-- =====================================================================
-- NOTE: no changes to any index. BiometricAttendanceLogs' existing unique
-- indexes (IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime and
-- IX_BiometricAttendanceLogs_Device_TransactionId) continue to provide full
-- idempotency for eSSL-sourced rows from monthly tables too - what changed
-- is only the VALUE written into DeviceTransactionId for a monthly-table row
-- (now table-qualified, e.g. "ESSL-DeviceLogs_8_2026-91", instead of just
-- "ESSL-91" - see EsslAttendanceSyncService.BuildDeviceTransactionId), not
-- the index shape itself. Rows already imported from the bare "DeviceLogs"
-- table keep their original "ESSL-{DeviceLogId}" value unchanged - this
-- script does not rewrite any existing data.
-- =====================================================================
